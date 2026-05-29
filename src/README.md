# src 目录说明

Codex 开工后请在这里创建 .NET solution 和项目结构。

推荐命令：

```bash
dotnet new sln -n AiOrchestrator
mkdir -p src
cd src
dotnet new webapi -n AiOrchestrator.Api
dotnet new classlib -n AiOrchestrator.Domain
dotnet new classlib -n AiOrchestrator.Application
dotnet new classlib -n AiOrchestrator.Infrastructure
dotnet new classlib -n AiOrchestrator.Workflow
dotnet new classlib -n AiOrchestrator.Agents
dotnet new classlib -n AiOrchestrator.Skills
dotnet new worker -n AiOrchestrator.Worker
cd ..
dotnet sln add src/*/*.csproj
```
