namespace LanguageExt;

public class Size
{
    /// <summary>
    /// Gets the sum of the two sizes and then finds the next power-of-two value higher 
    /// </summary>
    public static int Find(int size)
    {
        size--;
        size |= size >> 1;
        size |= size >> 2;
        size |= size >> 4;
        size |= size >> 8;
        size |= size >> 16;
        size++;
        return size;
    }
}