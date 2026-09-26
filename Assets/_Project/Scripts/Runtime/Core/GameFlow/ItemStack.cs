namespace Plunderspell.Core
{
    public class ItemStack
    {
        public ItemDefinition Definition { get; }
        public int Count { get; set; }

        public ItemStack(ItemDefinition definition, int count)
        {
            Definition = definition;
            Count = count;
        }
    }
}
