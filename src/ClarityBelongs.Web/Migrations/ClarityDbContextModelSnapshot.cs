using ClarityBelongs.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace ClarityBelongs.Web.Migrations;

[DbContext(typeof(ClarityDbContext))]
partial class ClarityDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");
        ClarityDbContext.ConfigureModel(modelBuilder);
    }
}
