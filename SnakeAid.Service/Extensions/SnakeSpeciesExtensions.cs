using SnakeAid.Core.Domains;
using Microsoft.EntityFrameworkCore;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;

namespace SnakeAid.Service.Extensions;

/// <summary>
/// Extension methods for SnakeSpecies to compute merged FirstAidGuideline
/// </summary>
public static class SnakeSpeciesExtensions
{
    /// <summary>
    /// Get the computed FirstAidContent for this species by merging base guideline from PrimaryVenomType with override
    /// </summary>
    /// <param name="species">Snake species with SpeciesVenoms loaded</param>
    /// <returns>Merged FirstAidContent or null if no guideline available</returns>
    public static FirstAidContent? GetMergedFirstAidContent(this SnakeSpecies species)
    {
        // 1. Get base guideline from PrimaryVenomType
        FirstAidContent? baseContent = null;

        if (species.PrimaryVenomType.HasValue && species.SpeciesVenoms?.Any() == true)
        {
            // Map PrimaryVenomType enum to VenomType.Id
            // Neurotoxic = 0 → VenomType Id = 1
            // Hemotoxic = 1 → VenomType Id = 2
            // Cytotoxic = 2 → VenomType Id = 3
            // Myotoxic = 3 → VenomType Id = 4
            int primaryVenomTypeId = (int)species.PrimaryVenomType.Value + 1;

            // Find the VenomType matching PrimaryVenomType ID
            var primaryVenom = species.SpeciesVenoms
                .FirstOrDefault(sv => sv.VenomTypeId == primaryVenomTypeId);

            if (primaryVenom?.VenomType?.FirstAidGuideline?.Content != null)
            {
                // Clone base content to avoid modifying original
                baseContent = CloneFirstAidContent(primaryVenom.VenomType.FirstAidGuideline.Content);
            }
        }

        // If no primary venom found, try to get any available guideline as fallback
        if (baseContent == null && species.SpeciesVenoms?.Any() == true)
        {
            var anyVenomWithGuideline = species.SpeciesVenoms
                .FirstOrDefault(sv => sv.VenomType?.FirstAidGuideline?.Content != null);

            if (anyVenomWithGuideline?.VenomType?.FirstAidGuideline?.Content != null)
            {
                baseContent = CloneFirstAidContent(anyVenomWithGuideline.VenomType.FirstAidGuideline.Content);
            }
        }

        // 2. If no override, return base content
        if (species.FirstAidGuidelineOverride == null)
        {
            return baseContent;
        }

        // 3. Apply override based on mode
        var overrideData = species.FirstAidGuidelineOverride;

        if (overrideData.Mode == OverrideMode.Replace)
        {
            // Replace mode: Use override content entirely
            return overrideData.Content;
        }
        else // OverrideMode.Append
        {
            // Append mode: Merge override into base
            if (baseContent == null)
            {
                // No base → just use override
                return overrideData.Content;
            }

            if (overrideData.Content == null)
            {
                // No override content → use base
                return baseContent;
            }

            // Merge: Add override fields to base
            var merged = baseContent; // Already cloned above

            if (overrideData.Content.Steps != null && overrideData.Content.Steps.Count > 0)
            {
                merged.Steps = merged.Steps ?? new List<FirstAidStep>();
                merged.Steps.AddRange(overrideData.Content.Steps);
            }

            if (overrideData.Content.Dos != null && overrideData.Content.Dos.Count > 0)
            {
                merged.Dos = merged.Dos ?? new List<FirstAidStep>();
                merged.Dos.AddRange(overrideData.Content.Dos);
            }

            if (overrideData.Content.Donts != null && overrideData.Content.Donts.Count > 0)
            {
                merged.Donts = merged.Donts ?? new List<FirstAidStep>();
                merged.Donts.AddRange(overrideData.Content.Donts);
            }

            if (overrideData.Content.Notes != null && overrideData.Content.Notes.Count > 0)
            {
                merged.Notes = merged.Notes ?? new List<string>();
                merged.Notes.AddRange(overrideData.Content.Notes);
            }

            return merged;
        }
    }

    /// <summary>
    /// Clone FirstAidContent to avoid modifying original
    /// </summary>
    private static FirstAidContent CloneFirstAidContent(FirstAidContent source)
    {
        return new FirstAidContent
        {
            Steps = source.Steps?.Select(s => new FirstAidStep { Text = s.Text, MediaUrl = s.MediaUrl }).ToList() ?? new List<FirstAidStep>(),
            Dos = source.Dos?.Select(s => new FirstAidStep { Text = s.Text, MediaUrl = s.MediaUrl }).ToList() ?? new List<FirstAidStep>(),
            Donts = source.Donts?.Select(s => new FirstAidStep { Text = s.Text, MediaUrl = s.MediaUrl }).ToList() ?? new List<FirstAidStep>(),
            Notes = source.Notes?.ToList() ?? new List<string>()
        };
    }
}
