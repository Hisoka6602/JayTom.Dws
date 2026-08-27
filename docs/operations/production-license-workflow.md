# DWS v2 生产授权工作流

GitHub Actions 工作流 `.github/workflows/production-license.yml` 使用 RSA-PSS/SHA-256 私钥签发授权文件，并立即使用对应公钥验签。验签失败、密钥不配对、机器码格式错误或信任根未生成时，工作流都会失败且不会上传产物。

## 首次配置

1. 在受控的离线 Windows 设备生成密钥对：

   ```powershell
   dotnet run --project .\JayTom.Dws.LicenseTool\JayTom.Dws.LicenseTool.csproj -c Release --framework net10.0-windows -p:Platform=x64 -- --generate-key-pair --private-key-output .\dws-license-private.pem --public-key-output .\dws-license-public.pem
   ```

2. 在 GitHub 仓库的 `production-license` Environment 中配置两个 Secret：

   - `DWS_LICENSE_PRIVATE_KEY_PEM`：离线私钥 PEM 的完整内容。
   - `DWS_LICENSE_PUBLIC_KEY_PEM`：与私钥配对的公钥 PEM 完整内容。

3. 为 `production-license` Environment 配置所需审核人，限制可运行分支。私钥不得提交到仓库、普通变量或工作流输入。

## 签发与部署

1. 从 GitHub Actions 手动运行“生成 DWS 生产授权文件”。机器码必须是新版客户端显示的 64 位十六进制 SHA-256 值。
2. 下载工作流产物。产物包含授权文件、`license-manifest.json` 和 `license-trust/<keyId>.pem`，不包含私钥。
3. 将 `.key` 文件放入 DWS 程序目录的 `License` 文件夹。
4. 将产物中的 `license-trust` 目录复制到 DWS 程序根目录。客户端会按授权包中的 `keyId` 加载对应公钥。
5. 可使用 `license-manifest.json` 中的 SHA-256 摘要核对传输完整性。

## 密钥轮换与撤销

- 轮换时生成新密钥对，更新 Environment 的两个 Secret；工作流会自动产生新的 `keyId` 和信任根文件。
- 旧授权仍需可用时，同时保留旧的 `license-trust/<oldKeyId>.pem`。
- 撤销旧密钥时，把旧 `keyId` 写入程序根目录 `license-trust/revoked-keys.txt`，或设置 `DWS_LICENSE_REVOKED_KEY_IDS`。
