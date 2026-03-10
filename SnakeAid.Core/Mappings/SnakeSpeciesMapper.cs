using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.SnakeSpecies;

namespace SnakeAid.Core.Mappings
{
    public class SnakeSpeciesMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<SnakeSpecies, SnakeSpeciesResponse>();
        }
    }
}