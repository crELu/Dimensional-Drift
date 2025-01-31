
// Base class
public interface IAugment
{
    public int MaxStacks { get; }
    public int Stacks { get; }
    
    public AllStats GetStats();
}
