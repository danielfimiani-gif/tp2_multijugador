using Fusion;

public struct NetInput : INetworkInput
{
    public NetworkButtons Buttons;
    public sbyte Horizontal;
}

public enum InputButton
{
    Jump = 0,
    Attack = 1
}
