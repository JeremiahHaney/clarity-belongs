using ClarityBelongs.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBelongs.Web.Migrations;

[DbContext(typeof(ClarityDbContext))]
[Migration("202609050002_AddContactSubmissions")]
partial class AddContactSubmissions
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");
        ClarityDbContext.ConfigureModel(modelBuilder);
    }
}
