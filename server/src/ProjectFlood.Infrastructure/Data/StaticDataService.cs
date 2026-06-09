using ProjectFlood.Domain.Interfaces;

namespace ProjectFlood.Infrastructure.Data;

public partial class StaticDataService : IStaticDataService
{
    public StaticDataService()
    {
        InitGeneratedData(Path.Combine(AppContext.BaseDirectory, "generated", "data"));
    }
}
