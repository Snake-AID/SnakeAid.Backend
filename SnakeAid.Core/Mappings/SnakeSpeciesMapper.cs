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
            config.NewConfig<SnakeSpecies, SnakeSpeciesResponse>()
                .Map(dest => dest.PrimaryVenomType, src => GetPrimaryVenomTypeLabel(src));

            config.NewConfig<SnakeSpecies, ListSnakeSpeciesResponse>()
                .Map(dest => dest.PrimaryVenomType, src => GetPrimaryVenomTypeLabel(src));

            config.NewConfig<SnakeSpecies, DetailSnakeSpeciesResponse>()
                .Map(dest => dest.PrimaryVenomType, src => GetPrimaryVenomTypeLabel(src));
        }

        private static string GetPrimaryVenomTypeLabel(SnakeSpecies src)
        {
            if (src.PrimaryVenomTypeDefinition != null && !string.IsNullOrEmpty(src.PrimaryVenomTypeDefinition.ScientificName))
            {
                return src.PrimaryVenomTypeDefinition.ScientificName;
            }

            if (src.PrimaryVenomTypeId.HasValue && src.SpeciesVenoms != null)
            {
                var match = src.SpeciesVenoms.FirstOrDefault(sv => sv.VenomTypeId == src.PrimaryVenomTypeId.Value);
                if (match?.VenomType != null && !string.IsNullOrEmpty(match.VenomType.ScientificName))
                {
                    return match.VenomType.ScientificName;
                }
            }

            return "None";
        }
    }
}