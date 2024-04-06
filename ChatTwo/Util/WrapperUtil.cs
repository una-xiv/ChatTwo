﻿using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Internal.Notifications;

namespace ChatTwo.Util;

public static class WrapperUtil
{
    public static void AddNotification(string content, NotificationType type, bool minimized = true)
    {
        Plugin.Notification.AddNotification(new Notification { Content = content, Type = type, Minimized = minimized });
    }
}