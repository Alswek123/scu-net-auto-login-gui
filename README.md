# SCUNET 配置器

在 Windows 上填写四川大学校园网账号、保存登录设置，并管理登录 Windows 后自动连接。界面使用 C# 和 Windows Forms，独立配置器只有几十 KB。

## 下载和使用

1. 打开 [最新版本](https://github.com/Alswek123/scu-net-auto-login-gui/releases/latest)，下载 `SCUNET-Windows-x64.zip`，完整解压到一个可以写入的目录。
2. 双击 `SCUNET_Config_Tool.exe`，填写账号、密码和运营商，点击 **保存配置**。
3. 在校园网环境中点击 **测试登录**，登录结果显示在登录器窗口。需要自动连接时，到 **登录与自启** 设置延迟并点击 **启用 / 更新**。下次登录 Windows 时生效。

已有登录器的用户可以单独下载 `SCUNET_Config_Tool.exe`，放进原目录替换配置器，或使用 **选择文件** 打开已有的登录器。移走目录或修改自启延迟后，点击 **启用 / 更新** 让计划任务采用新设置。

## 兼容性

| 登录程序 | 配置文件 | 编辑范围 |
| --- | --- | --- |
| `Sal.CLI.exe` | `config/config.json` | 账号、运营商、Wi-Fi、热点、登录等待、重试 |
| `scu_net_auto_login.exe` | `Config/setting.json` | 以上各项及旧核心的断线重连、代理、命令和 Cookie |

界面中的等待时间统一用秒。配置器自动转换新版登录器使用的毫秒，并保留已有账号列表中的其他账号和额外字段。若新版配置尚未创建、同目录已有旧配置，打开新版登录器会带入原有账号和通用网络选项，点击保存后写入新版文件。

账号密码保存在登录器的本地 JSON 中。请勿分享配置文件。配置格式损坏或打开后被其他程序修改时，配置器会提示重新读取。

## 从源码构建

在 Windows 10/11 上用 PowerShell 执行 `./build.ps1`。脚本调用系统 .NET Framework 的 C# 编译器，输出 `build/SCUNET_Config_Tool.exe`。无需 Godot 或 NuGet 依赖。

配置器采用 [MIT 许可证](LICENSE)。完整包包含上游 v2.1.8 Windows x64 命令行登录器及其 MIT 许可证。校园网认证由上游程序执行。[上游源码与发布记录](https://github.com/LFWQSP2641/scu_net_auto_login/releases/tag/v2.1.8)
