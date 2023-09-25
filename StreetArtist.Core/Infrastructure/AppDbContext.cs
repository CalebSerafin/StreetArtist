using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using StreetArtist.Core.Infrastructure.StronglyTypedIds;

namespace StreetArtist.Core.Infrastructure;
internal class AppDbContext : DbContext {
    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        StrongIdEFCoreAutoConverter.OnModelCreating(modelBuilder);



    }

    /// <inheritdoc/>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
        base.ConfigureConventions(configurationBuilder);


    }

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder options) {
        base.OnConfiguring(options);
    }
}
