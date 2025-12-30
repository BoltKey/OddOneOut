using OddOneOut.Data;

public static class DataSeeder
{
    public static void SeedWordCards(AppDbContext context)
    {
        if (context.WordCard.Any()) return;

        // 1. Define your data compactly
        var categories = new[]
        {
            new { Name = "Fruit", Words = new[] { "Apple", "Banana", "Orange", "Grape", "Mango" } },
            new { Name = "Cars",  Words = new[] { "Tesla", "Ford", "BMW", "Honda", "Fiat" } },
            new { Name = "Tech",  Words = new[] { "Mouse", "Keyboard", "Screen", "Laptop" } }
        };

        var entities = new List<WordCard>();

        // 2. Iterate and flatten into objects
        foreach (var cat in categories)
        {
            foreach (var word in cat.Words)
            {
                entities.Add(new WordCard
                {
                    Id = Guid.NewGuid(),
                    Category = cat.Name,
                    Word = word
                });
            }
        }

        // 3. Bulk insert
        context.WordCard.AddRange(entities);
        context.SaveChanges();
    }
}