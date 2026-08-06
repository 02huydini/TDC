[System.Flags]
public enum ElementType
{
    None,
    Ice,
    Fire,
    Water,
    Lightning
}

[System.Flags]
public enum AbilityType
{
    None,
    Spawn,
    DeadSpawn,
    Shield,
    Heal,
    Sprint,
    Overcharge
}

[System.Flags]
public enum StatusType
{
    None,
    Frozen,
    Electrocuted,
    Burning,
    Stunned,
    Overcharged,
    Sprinting,
    Shielded
}

[System.Flags]
public enum BoonType
{
    Power,
    Swiftness,
    Farsight,
    Fortune
}

// "Đổi mục tiêu của Tower: ...Khoảng cách xa nhất/Khoảng cách gần nhất/HP cao nhất/HP thấp nhất"
public enum TargetMode
{
    Nearest,
    Farthest,
    HighestHP,
    LowestHP
}

// "Switch AS mode: Đổi chế độ của Active Skill cho Tower giữa tự động/thủ công kích hoạt."
public enum ActiveSkillMode
{
    Manual,
    Automatic
}
