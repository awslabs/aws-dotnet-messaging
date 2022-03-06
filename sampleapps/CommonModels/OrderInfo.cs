namespace CommonModels
{
    public class OrderInfo
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string OrderId { get; set; }

        public string UserId { get; set; }

        public IList<Item> Items { get; set; } = new List<Item>();

        public class Item
        {
            public string ProductId { get; set; }
            public int Count { get; set; }
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}