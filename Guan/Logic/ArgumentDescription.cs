namespace Guan.Logic
{
    /// <summary>
    /// Meta data about term argument.
    /// </summary>
    public class ArgumentDescription
    {
        public string Text;
        public bool Required;

        public ArgumentDescription(string text, bool required)
        {
            Text = text;
            Required = required;
        }
    }
}
