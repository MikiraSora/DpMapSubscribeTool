using System;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DpMapSubscribeTool.Models;

public partial class MapSubscribe : ObservableObject
{
    private Regex cachedRuleRegex;

    [ObservableProperty]
    private bool enable;

    [ObservableProperty]
    private string matchContent;

    [ObservableProperty]
    private MapSubscribeRule matchRule;

    [ObservableProperty]
    private string name;

    partial void OnMatchRuleChanged(MapSubscribeRule value)
    {
        cachedRuleRegex = default;
    }

    public bool CheckRule(Server server)
    {
        if (!Enable)
            return false;

        if (string.IsNullOrWhiteSpace(server.Map))
            return false;

        switch (MatchRule)
        {
            case MapSubscribeRule.CustomRegex:
                cachedRuleRegex ??= new Regex(MatchContent);
                return cachedRuleRegex.IsMatch(server.Map);
            default:
                throw new ArgumentOutOfRangeException(nameof(MatchRule));
        }
    }
}