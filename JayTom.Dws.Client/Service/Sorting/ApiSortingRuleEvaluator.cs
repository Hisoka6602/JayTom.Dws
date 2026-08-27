using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Integrations;
using Newtonsoft.Json;
using UploadResponse = JayTom.Dws.Integrations.Contracts.UploadResponse;

namespace JayTom.Dws.Client.Service.Sorting;

/// <summary>拥有 API 分拣规则解析、优先级快照和格口索引。</summary>
internal sealed class ApiSortingRuleEvaluator
{
    /// <summary>按优先级预解析的不可变规则快照。</summary>
    private ApiRuleSnapshot[] _rules = [];

    /// <summary>按 API 配置标识预计算的目标格口索引。</summary>
    private IReadOnlyDictionary<long, ApiSortingInfoModel> _sortingLookup =
        new Dictionary<long, ApiSortingInfoModel>();

    /// <summary>原子替换由同一批配置构建的规则和目标索引。</summary>
    public void Replace(
        IEnumerable<ApiRuleInfoModel> rules,
        IEnumerable<ApiSortingInfoModel> sortingConfigurations)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(sortingConfigurations);
        ApiRuleSnapshot[] ruleSnapshot = rules
            .Select(rule => new ApiRuleSnapshot(rule, TryParseRule(rule.JsonContent)))
            .Where(snapshot => snapshot.Definition is not null)
            .OrderByDescending(snapshot => snapshot.Definition!.IsUseStringComparison)
            .ThenByDescending(snapshot => snapshot.Definition!.IsUseJsonField)
            .ThenByDescending(snapshot => snapshot.Definition!.IsUseStringSearch)
            .ThenBy(snapshot => snapshot.Definition!.ResponseStatus)
            .ToArray();
        IReadOnlyDictionary<long, ApiSortingInfoModel> sortingLookup = sortingConfigurations
            .GroupBy(sorting => sorting.Id)
            .ToDictionary(group => group.Key, group => group.Last());
        Volatile.Write(ref _rules, ruleSnapshot);
        Volatile.Write(ref _sortingLookup, sortingLookup);
    }

    /// <summary>根据外部响应解析第一条命中规则对应的目标格口。</summary>
    public long? ResolveExitId(UploadResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        ApiRuleSnapshot? rule = FindRule(response);
        if (rule is null ||
            !Volatile.Read(ref _sortingLookup).TryGetValue(
                rule.Rule.ApiSortingId,
                out ApiSortingInfoModel? sorting))
        {
            return null;
        }
        return sorting.ExitId;
    }

    /// <summary>按配置优先级查找首条匹配规则，并让全部 JSON 规则共享一次解析。</summary>
    private ApiRuleSnapshot? FindRule(UploadResponse response)
    {
        JsonDocument? responseDocument = null;
        bool responseParseAttempted = false;
        try
        {
            foreach (ApiRuleSnapshot snapshot in Volatile.Read(ref _rules))
            {
                ApiRuleJsonDto? definition = snapshot.Definition;
                JsonElement? responseRoot = null;
                if (definition is { IsUseStringComparison: true, IsUseJsonField: true })
                {
                    if (!responseParseAttempted)
                    {
                        responseDocument = TryParseResponse(response.ResponseContent);
                        responseParseAttempted = true;
                    }
                    responseRoot = responseDocument?.RootElement;
                }
                if (ValidateRule(response, definition, responseRoot))
                {
                    return snapshot;
                }
            }
        }
        finally
        {
            responseDocument?.Dispose();
        }
        return null;
    }

    /// <summary>使用已经共享解析的响应文档校验单条 API 规则。</summary>
    private static bool ValidateRule(
        UploadResponse response,
        ApiRuleJsonDto? definition,
        JsonElement? responseRoot)
    {
        if (definition is null ||
            definition.ResponseStatus !=
            (response.IsSuccess ? UploadStatus.Succeeded : UploadStatus.Failed))
        {
            return false;
        }
        if (!definition.IsUseStringComparison)
        {
            return true;
        }
        if (definition.IsUseStringSearch)
        {
            return definition.SearchDirection == SearchDirection.Forward
                ? response.ResponseContent.IndexOf(
                    definition.SearchStringContent,
                    StringComparison.Ordinal) >= 0
                : response.ResponseContent.LastIndexOf(
                    definition.SearchStringContent,
                    StringComparison.Ordinal) >= 0;
        }
        if (!definition.IsUseJsonField || responseRoot is null)
        {
            return false;
        }
        JsonElement? fieldValue = FindFieldValue(
            responseRoot.Value,
            definition.JsonField,
            definition.SearchDirection);
        return fieldValue.HasValue &&
               string.Equals(
                   fieldValue.Value.ToString(),
                   definition.JsonFieldValue,
                   StringComparison.Ordinal);
    }

    /// <summary>清理接口响应中的控制字符并只解析一次 JSON 文档。</summary>
    private static JsonDocument? TryParseResponse(string responseContent)
    {
        try
        {
            string unescapedContent = Regex.Unescape(responseContent);
            string sanitizedContent = Regex.Replace(
                unescapedContent,
                @"[\u0000-\u001D\b]",
                string.Empty);
            return JsonDocument.Parse(sanitizedContent);
        }
        catch (Exception exception) when (
            exception is ArgumentException or System.Text.Json.JsonException)
        {
            NLog.LogManager.GetCurrentClassLogger()
                .Warn(exception, "API 响应不是有效 JSON");
            return null;
        }
    }

    /// <summary>解析单条 API 规则；无效配置不会进入响应热路径。</summary>
    private static ApiRuleJsonDto? TryParseRule(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<ApiRuleJsonDto>(json);
        }
        catch (Exception exception)
        {
            NLog.LogManager.GetCurrentClassLogger().Error(exception, "API 分拣规则解析失败");
            return null;
        }
    }

    /// <summary>按搜索方向遍历响应树并读取指定字段。</summary>
    private static JsonElement? FindFieldValue(
        JsonElement root,
        string fieldName,
        SearchDirection direction)
    {
        var stack = new Stack<JsonElement>();
        stack.Push(root);
        JsonElement? lastMatch = null;
        while (stack.Count > 0)
        {
            JsonElement element = stack.Pop();
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(fieldName, out JsonElement field))
            {
                lastMatch = field;
                if (direction == SearchDirection.Forward)
                {
                    return field;
                }
            }
            if (element.ValueKind == JsonValueKind.Object)
            {
                JsonProperty[] properties = element.EnumerateObject().ToArray();
                IEnumerable<JsonProperty> ordered = direction == SearchDirection.Forward
                    ? properties.Reverse()
                    : properties;
                foreach (JsonProperty property in ordered)
                {
                    stack.Push(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                JsonElement[] items = element.EnumerateArray().ToArray();
                IEnumerable<JsonElement> ordered = direction == SearchDirection.Forward
                    ? items.Reverse()
                    : items;
                foreach (JsonElement item in ordered)
                {
                    stack.Push(item);
                }
            }
        }
        return lastMatch;
    }
}
