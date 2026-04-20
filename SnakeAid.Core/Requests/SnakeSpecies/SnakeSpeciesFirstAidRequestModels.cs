using System;
using System.Collections.Generic;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.SnakeSpecies;

public class SnakeSpeciesFirstAidOverrideRequest
{
    public OverrideMode Mode { get; set; } = OverrideMode.Append;
    public SnakeSpeciesFirstAidContentRequest Content { get; set; } = new();
}

public class SnakeSpeciesFirstAidContentRequest
{
    public List<SnakeSpeciesFirstAidStepRequest> Steps { get; set; } = new();
    public List<SnakeSpeciesFirstAidStepRequest> Dos { get; set; } = new();
    public List<SnakeSpeciesFirstAidStepRequest> Donts { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

public class SnakeSpeciesFirstAidStepRequest
{
    public string Text { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public Guid? MediaId { get; set; }
}
