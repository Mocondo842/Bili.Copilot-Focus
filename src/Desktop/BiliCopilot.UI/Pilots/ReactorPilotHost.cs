// Copyright (c) Bili Copilot. All rights reserved.

using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hosting;
using static Microsoft.UI.Reactor.Factories;

namespace BiliCopilot.UI.Pilots;

/// <summary>
/// Reactor 试点的最小宿主封装。
/// 目的只有一个：让编译器与裁剪器完整解析 Reactor 的类型引用链，
/// 从而证明「本项目可以引用并使用 Reactor」在工程层面成立。
/// 它不被任何页面使用，也不注册任何导航；真正的挂载与渲染需真机验证。
/// </summary>
internal static class ReactorPilotHost
{
    /// <summary>
    /// 创建一个承载最小 Reactor 组件的宿主控件。
    /// </summary>
    /// <returns>未被挂入视觉树的宿主控件.</returns>
    public static ReactorHostControl Create()
    {
        var host = new ReactorHostControl();
        host.Mount(new ReactorPilotComponent());
        return host;
    }
}

/// <summary>
/// 试点组件：渲染一行文字。
/// </summary>
internal sealed class ReactorPilotComponent : Component
{
    /// <inheritdoc/>
    public override Element Render()
        => Heading("Bili.Copilot · Reactor 试点");
}
