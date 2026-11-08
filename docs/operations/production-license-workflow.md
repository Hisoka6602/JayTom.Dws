# DWS 新旧版本生产授权工作流

GitHub Actions 工作流 `.github/workflows/production-license.yml` 提供 `license_format` 版本选择，默认 `v2`（新版）。已有项目使用 2025 年旧客户端时，选择 `legacy-v1`。版本与机器码长度不匹配时会失败，不会自动切换格式。

| 版本选项 | 目标客户端 | 机器码 | 校验与部署 |
| --- | --- | --- | --- |
| `v2`（默认） | 新版客户端 | 64 位 SHA-256 十六进制 | RSA-PSS/SHA-256 验签，部署 `.key` 和 `license-trust` |
| `legacy-v1` | 2025 年旧客户端，包括 2025-10-23 编译版本对应的历史协议 | 32 位十六进制 | 按旧版二进制协议生成并解密校验，部署 `.key` |

两种格式都使用 `.key` 扩展名，内部格式不同。版本选择应与目标客户端一致。截图中的“授权码不存在”是远程服务的返回；离线工作流生成文件后使用“导入授权文件”，不会把授权码注册到远程服务。

授权码可留空，工作流会安全生成 32 位授权码并屏蔽日志中的内容；也可以继续填写已有项目的授权码。新旧两种格式都支持此行为。

## 新版 v2 首次配置

1. 在受控的离线 Windows 设备生成密钥对：

   ```powershell
   dotnet run --project .\JayTom.Dws.LicenseTool\JayTom.Dws.LicenseTool.csproj -c Release --framework net10.0-windows -p:Platform=x64 -- --generate-key-pair --private-key-output .\dws-license-private.pem --public-key-output .\dws-license-public.pem
   ```

2. 在 GitHub 仓库的 `production-license` Environment 中配置两个 Secret：

   - `DWS_LICENSE_PRIVATE_KEY_PEM`：离线私钥 PEM 的完整内容。
   - `DWS_LICENSE_PUBLIC_KEY_PEM`：与私钥配对的公钥 PEM 完整内容。

3. 为 `production-license` Environment 配置所需审核人，限制可运行分支。私钥不得提交到仓库、普通变量或工作流输入。

## 签发与部署

1. 从 GitHub Actions 手动运行“生成 DWS 生产授权文件”，保持 `license_format = v2`。机器码必须是新版客户端显示的 64 位十六进制 SHA-256 值。
2. 下载工作流产物。产物包含授权文件、`license-manifest.json` 和 `license-trust/<keyId>.pem`，不包含私钥。
3. 将 `.key` 文件放入 DWS 程序目录的 `License` 文件夹。
4. 将产物中的 `license-trust` 目录复制到 DWS 程序根目录。客户端会按授权包中的 `keyId` 加载对应公钥。
5. 可使用 `license-manifest.json` 中的 SHA-256 摘要核对传输完整性。

## 给已上线旧项目生成授权

1. 运行同一工作流，将 `license_format` 改为 `legacy-v1`。
2. 填入旧客户端授权页面显示的 32 位机器码、授权码、客户名称和到期时间等信息，不能使用 runner 或开发机自己的机器码。
3. 下载产物，在旧客户端点击“导入授权文件”选择生成的 `.key`。也可以将文件放入旧程序目录的 `License` 文件夹，只保留要使用的授权文件。
4. 旧版产物包含 `.key` 和 `license-manifest.json`，无需 `license-trust`，也不需要配置 v2 的两个签名 Secret。工作流仍使用 `production-license` Environment 的运行和审核规则。

旧协议使用 RSA PKCS#1 分块加密，并在二进制文件中携带经 AES 加密的 XML 解密私钥和 AES 参数。这是旧客户端要求的历史格式，不具备 v2 的外置信任根签名保证。因此工具为每个旧版文件独立生成一次性密钥，绝不读取或写入 v2 生产签名私钥。生成后检查机器码、有效期和冻结状态，失败时不上传产物。

本地生成旧版文件（先以 Release/x64 构建授权工具）：

```powershell
.\eng\license\New-DwsProductionLicense.ps1 -LicenseFormat legacy-v1 -LicenseCode 'PROJECT-001' -MachineCode '0123456789ABCDEF0123456789ABCDEF' -CustomerName '旧版项目' -ExpirationDate '2027-12-31' -OutputPath '.\artifacts\legacy\License.key'
```

此兼容只用于给旧客户端生成文件；新版客户端仍使用 v2 签名校验。

## 工作流兼容性检查

每次签发前，工作流运行 `eng/license/Test-DwsLicenseCompatibility.ps1`：使用测试密钥检查默认 v2、显式旧版、2025 历史协议独立解密、中文长文本分块、机器码限制、冻结状态、畸形文件和密钥隔离。测试不读取生产 Secret，测试文件与密钥在完成后清理。

## 密钥轮换与撤销

- 轮换时生成新密钥对，更新 Environment 的两个 Secret；工作流会自动产生新的 `keyId` 和信任根文件。
- 旧授权仍需可用时，同时保留旧的 `license-trust/<oldKeyId>.pem`。
- 撤销旧密钥时，把旧 `keyId` 写入程序根目录 `license-trust/revoked-keys.txt`，或设置 `DWS_LICENSE_REVOKED_KEY_IDS`。
