using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CoreDbContext))]
[Migration("20261005100000_EnforceWorkflowCompanyIsolation")]
partial class EnforceWorkflowCompanyIsolation
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        // This migration changes PostgreSQL security policy only; the EF model is unchanged.
    }
}
