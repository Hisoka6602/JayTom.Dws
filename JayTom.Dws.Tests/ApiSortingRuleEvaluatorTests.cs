using System.Text.Json;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Integrations.Contracts;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Tests;

/// <summary>验证真实极昼响应中的嵌套 JSON 字符串和多格口搜索方向。</summary>
public sealed class ApiSortingRuleEvaluatorTests
{
    /// <summary>用户提供的真实响应：标签字段为 JSON 字符串，候选格口依次为 42、44。</summary>
    private const string OriginalResponseJson = """
        {"code":1,"msg":"success","data":{"id":"47776205","waybillNo":"JT5532127376096","packageNumber":null,"eventTime":"2026-09-30 20:17:08.767","orderId":"973211691657441024","rfid":null,"terminalDispatchCode":"610,F972-01,916","firstDispatchCode":"610","secondDispatchCode":"F972-01","thirdlyDispatchCode":"916","fourthDispatchCode":null,"stationDispatchCode":null,"pickNetworkCode":"1020568","destinationCode":"411200","collectCode":null,"collectTime":null,"deliveryCode":null,"orderTime":null,"orderPlatformSource":null,"recvProvinceName":null,"recvProvinceId":null,"recvCityName":null,"recvCityId":null,"recvDistrictName":null,"recvDistrictId":null,"recvTownName":null,"recvTownId":null,"sendProvinceName":null,"sendProvinceId":null,"sendCityName":null,"sendCityId":null,"sendDistrictName":null,"sendDistrictId":null,"sendTownName":null,"sendTownId":null,"customerCode":null,"villageCode":null,"stationCode":null,"productType":null,"orderSource":null,"orderType":null,"isChanged":0,"goodsWeight":null,"realTimeWeight":0,"packageProperty":null,"packNetworkCode":null,"unpackNetworkCode":null,"used":null,"createTime":"2026-09-30 20:18:40.769","orderLabelList":"[{\"labelTime\":\"2026-09-30 20:14:53.179\",\"mainLabel\":\"village\",\"removeFlag\":1,\"subLabel\":\"thd_village\"}]","source":"本地","sortingRangeStatus":null,"abnormalityCode":"111","abnormalityLabelCode":null,"matchPath":"SEGMENT","matchLevel":4,"codeList":[{"planDetailId":"960445060925628424","planId":"960442690577637438","sortingPortCode":"42","sortingPortType":1,"labelCode":"","subLabelCode":"","code1":"610","code2":"F972-01","code3":"916","code4":"","sortByCode":1,"sortingType":1,"nextSiteCode":"","nextSiteName":"","dispatcherCode":"YZ0001092831","dispatcherName":"拼多多进村中转仓","stationCode":"","stationName":"","provinceCode":"","provinceName":"","cityCode":"","cityName":"","districtCode":"","districtName":"","townshipCode":"","townshipName":"","villageCode":"","villageName":"","configContent":null,"packageCode":"","createTime":"2026-08-26 14:44:47","updateTime":"2026-08-26 14:44:47","gridCode":"111","operateType":2},{"planDetailId":"960445060925628425","planId":"960442690577637438","sortingPortCode":"44","sortingPortType":1,"labelCode":"","subLabelCode":"","code1":"610","code2":"F972-01","code3":"916","code4":"","sortByCode":1,"sortingType":1,"nextSiteCode":"","nextSiteName":"","dispatcherCode":"YZ0001092831","dispatcherName":"拼多多进村中转仓","stationCode":"","stationName":"","provinceCode":"","provinceName":"","cityCode":"","cityName":"","districtCode":"","districtName":"","townshipCode":"","townshipName":"","villageCode":"","villageName":"","configContent":null,"packageCode":"","createTime":"2026-08-26 14:44:47","updateTime":"2026-08-26 14:44:47","gridCode":"111","operateType":2}]},"traceId":"c736a302-4759-4a0e-ae67-54bcb2f261de","succ":true,"fail":false}
        """;

    /// <summary>用户补充的单格口响应：只有 44，仍包含需要原样保留的标签转义。</summary>
    private const string SingleChuteResponseJson = """
        {"code":1,"msg":"success","data":{"id":"49188703","waybillNo":"JT5531859332128","packageNumber":null,"eventTime":"2026-10-01 09:30:49.580","orderId":"973411329027058304","rfid":null,"terminalDispatchCode":"610,F972-01,916","firstDispatchCode":"610","secondDispatchCode":"F972-01","thirdlyDispatchCode":"916","fourthDispatchCode":null,"stationDispatchCode":null,"pickNetworkCode":"6377100","destinationCode":"411200","collectCode":null,"collectTime":null,"deliveryCode":null,"orderTime":null,"orderPlatformSource":null,"recvProvinceName":null,"recvProvinceId":null,"recvCityName":null,"recvCityId":null,"recvDistrictName":null,"recvDistrictId":null,"recvTownName":null,"recvTownId":null,"sendProvinceName":null,"sendProvinceId":null,"sendCityName":null,"sendCityId":null,"sendDistrictName":null,"sendDistrictId":null,"sendTownName":null,"sendTownId":null,"customerCode":null,"villageCode":null,"stationCode":null,"productType":null,"orderSource":null,"orderType":null,"isChanged":0,"goodsWeight":null,"realTimeWeight":0,"packageProperty":null,"packNetworkCode":null,"unpackNetworkCode":null,"used":null,"createTime":"2026-10-01 09:32:21.508","orderLabelList":"[{\"labelTime\":\"2026-10-01 09:28:09.132\",\"mainLabel\":\"village\",\"removeFlag\":1,\"subLabel\":\"thd_village\"}]","source":"本地","sortingRangeStatus":null,"abnormalityCode":"111","abnormalityLabelCode":null,"matchPath":"SEGMENT","matchLevel":4,"codeList":[{"planDetailId":"960445060925628425","planId":"960442690577637438","sortingPortCode":"44","sortingPortType":1,"labelCode":"","subLabelCode":"","code1":"610","code2":"F972-01","code3":"916","code4":"","sortByCode":1,"sortingType":1,"nextSiteCode":"","nextSiteName":"","dispatcherCode":"YZ0001092831","dispatcherName":"拼多多进村中转仓","stationCode":"","stationName":"","provinceCode":"","provinceName":"","cityCode":"","cityName":"","districtCode":"","districtName":"","townshipCode":"","townshipName":"","villageCode":"","villageName":"","configContent":null,"packageCode":"","createTime":"2026-08-26 14:44:47","updateTime":"2026-08-26 14:44:47","gridCode":"111","operateType":2}]},"traceId":"108bdecc-fa3d-4077-9d95-766e4bce3246","succ":true,"fail":false}
        """;

    /// <summary>补充的完整响应只有 44，正向和反向均必须命中 44。</summary>
    [Theory]
    [InlineData(SearchDirection.Forward)]
    [InlineData(SearchDirection.Backward)]
    public void Single_chute_original_response_matches_44_in_both_directions(SearchDirection direction)
    {
        var evaluator = CreateEvaluator("44", direction);
        Assert.Equal(44L, evaluator.ResolveExitId(CreateResponse(SingleChuteResponseJson)));
    }

    /// <summary>合法标签字符串不能破坏整份响应，正向读取第一候选格口。</summary>
    [Fact]
    public void Original_response_with_escaped_label_matches_forward_42()
    {
        var evaluator = CreateEvaluator("42", SearchDirection.Forward);
        Assert.Equal(42L, evaluator.ResolveExitId(CreateResponse(OriginalResponseJson)));
    }

    /// <summary>候选数组仅有 44 时应命中 44，避免特殊标签只影响部分包裹。</summary>
    [Fact]
    public void Escaped_label_does_not_prevent_single_chute_44()
    {
        var evaluator = CreateEvaluator("44", SearchDirection.Forward);
        var response = """
            {"code":1,"data":{"orderLabelList":"[{\"mainLabel\":\"village\"}]","codeList":[{"sortingPortCode":"44"}]}}
            """;
        Assert.Equal(44L, evaluator.ResolveExitId(CreateResponse(response)));
    }

    /// <summary>反向搜索应读取最后一个同名字段，而不是又回到第一个字段。</summary>
    [Theory]
    [InlineData(SearchDirection.Forward, "42", 42L)]
    [InlineData(SearchDirection.Backward, "44", 44L)]
    public void Direction_selects_first_or_last_candidate(
        SearchDirection direction, string expectedValue, long expectedExitId)
    {
        var evaluator = CreateEvaluator(expectedValue, direction);
        var response = """
            {"code":1,"data":{"codeList":[{"sortingPortCode":"42"},{"sortingPortCode":"44"}]}}
            """;
        Assert.Equal(expectedExitId, evaluator.ResolveExitId(CreateResponse(response)));
    }

    /// <summary>原始复杂响应也必须支持反向选择 44。</summary>
    [Fact]
    public void Original_response_matches_backward_44()
    {
        var evaluator = CreateEvaluator("44", SearchDirection.Backward);
        Assert.Equal(44L, evaluator.ResolveExitId(CreateResponse(OriginalResponseJson)));
    }

    /// <summary>正向规则保留第一候选语义，不会自动跳过 42 去选择 44。</summary>
    [Fact]
    public void Forward_44_does_not_select_second_candidate()
    {
        var evaluator = CreateEvaluator("44", SearchDirection.Forward);
        Assert.Null(evaluator.ResolveExitId(CreateResponse(OriginalResponseJson)));
    }

    /// <summary>同级规则都有效时，维持先命中规则优先的既有行为。</summary>
    [Fact]
    public void Earlier_forward_42_rule_remains_preferred_over_backward_44()
    {
        var evaluator = new ApiSortingRuleEvaluator();
        evaluator.Replace(
            [CreateRule(42, "42", SearchDirection.Forward),
             CreateRule(44, "44", SearchDirection.Backward)],
            [new ApiSortingInfoModel { Id = 42, ExitId = 42 },
             new ApiSortingInfoModel { Id = 44, ExitId = 44 }]);
        Assert.Equal(42L, evaluator.ResolveExitId(CreateResponse(OriginalResponseJson)));
    }

    /// <summary>既有外层字符串封装与额外反斜杠转义的响应仍可兼容。</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Legacy_encoded_response_is_supported(bool isJsonString)
    {
        var evaluator = CreateEvaluator("44", SearchDirection.Backward);
        var response = isJsonString
            ? JsonSerializer.Serialize(OriginalResponseJson)
            : OriginalResponseJson.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
        Assert.Equal(44L, evaluator.ResolveExitId(CreateResponse(response)));
    }

    /// <summary>传输夹带的控制字符可清理，但合法字段值中的转义必须保留。</summary>
    [Fact]
    public void Transport_control_characters_do_not_destroy_json_escapes()
    {
        var evaluator = CreateEvaluator("42", SearchDirection.Forward);
        Assert.Equal(42L, evaluator.ResolveExitId(CreateResponse("\0" + OriginalResponseJson)));
    }

    /// <summary>真正无效的响应必须维持未命中，不能触发成功格口。</summary>
    [Fact]
    public void Invalid_json_does_not_match_success_field_rule()
    {
        var evaluator = CreateEvaluator("44", SearchDirection.Forward);
        Assert.Null(evaluator.ResolveExitId(CreateResponse("{not-json}")));
    }

    /// <summary>成功响应规则仍不能匹配失败的 HTTP 或业务响应。</summary>
    [Fact]
    public void Failed_response_does_not_match_success_rule()
    {
        var evaluator = CreateEvaluator("42", SearchDirection.Forward);
        Assert.Null(evaluator.ResolveExitId(new UploadResponse
        {
            IsSuccess = false, ResponseContent = OriginalResponseJson
        }));
    }

    /// <summary>创建单条测试规则及其对应格口索引。</summary>
    private static ApiSortingRuleEvaluator CreateEvaluator(string fieldValue, SearchDirection direction)
    {
        var evaluator = new ApiSortingRuleEvaluator();
        evaluator.Replace(
            [CreateRule(44, fieldValue, direction)],
            [new ApiSortingInfoModel { Id = 44, ExitId = long.Parse(fieldValue) }]);
        return evaluator;
    }

    /// <summary>创建与界面配置一致的成功响应 JSON 字段规则。</summary>
    private static ApiRuleInfoModel CreateRule(long sortingId, string fieldValue, SearchDirection direction) =>
        new()
        {
            Id = sortingId,
            ApiSortingId = sortingId,
            JsonContent = JsonSerializer.Serialize(new ApiRuleJsonDto
            {
                ResponseStatus = UploadStatus.Succeeded,
                IsUseStringComparison = true,
                IsUseJsonField = true,
                JsonField = "sortingPortCode",
                JsonFieldValue = fieldValue,
                SearchDirection = direction
            })
        };

    /// <summary>构造已经被接口判定为成功的原始响应。</summary>
    private static UploadResponse CreateResponse(string content) => new()
    {
        IsSuccess = true, ResponseContent = content
    };
}
