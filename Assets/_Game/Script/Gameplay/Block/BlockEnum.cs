public enum EBlockColorType
{
    None = -1,
    Red = 0,
    Pink = 1,
    Brown = 2,
    Peach = 3,
    Yellow = 4,
    Orange = 5,
    Purple = 6,
    DarkPink = 7,
    Green = 8,
    LimeGreen = 9,
    Blue = 10,
    Cyan = 11,
    DarkPurple = 12,
    Teal = 13,
}

public enum EBlockState
{
    Idle = 0,       // Trạng thái nguyên khối (solid)
    Unloading = 1,  // Đang bung ra để dỡ hàng (opened for unloading)
    Receiving = 2,  // Đang nhận cube từ ngoài vào (receiving cubes)
}
