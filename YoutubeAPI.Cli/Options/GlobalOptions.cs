using System.CommandLine;

namespace YoutubeAPI.Cli.Options;

public sealed record GlobalOptions(
    string? CookiesPath,
    string Language,
    string Region,
    int AuthUser,
    string? PageId,
    string? VisitorData,
    string? RolloutToken,
    string? ProofOfOriginToken,
    string Format,
    bool Pretty,
    string? Continuation,
    int Pages,
    bool All,
    bool HasExplicitPages,
    bool HasExplicitContinuation)
{
    public static OptionDefinitions CreateDefinitions()
    {
        var cookiesOption = new Option<string?>("--cookies")
            { Recursive = true, Description = "Path to Netscape-formatted cookie file." };
        var hlOption = new Option<string>("--hl")
            { Recursive = true, DefaultValueFactory = _ => "en", Description = "Language code (e.g. 'en')." };
        var glOption = new Option<string>("--gl")
            { Recursive = true, DefaultValueFactory = _ => "US", Description = "Region/country code (e.g. 'US')." };
        var authUserOption = new Option<int>("--auth-user")
            { Recursive = true, DefaultValueFactory = _ => 0, Description = "Auth user index." };
        var pageIdOption = new Option<string?>("--page-id")
            { Recursive = true, Description = "Brand account page ID." };
        var visitorDataOption = new Option<string?>("--visitor-data")
            { Recursive = true, Description = "Visitor data token." };
        var rolloutTokenOption = new Option<string?>("--rollout-token")
            { Recursive = true, Description = "Rollout token." };
        var poTokenOption = new Option<string?>("--po-token")
            { Recursive = true, Description = "Proof-of-Origin token." };
        var formatOption = new Option<string>("--format")
        {
            Recursive = true, DefaultValueFactory = _ => "json", Description = "Output format: json, ndjson, or table."
        };
        var prettyOption = new Option<bool>("--pretty")
            { Recursive = true, Description = "Format JSON output with indentation." };
        var continuationOption = new Option<string?>("--continuation")
            { Recursive = true, Description = "Opaque continuation token for pagination." };
        var pagesOption = new Option<int>("--pages")
        {
            Recursive = true, DefaultValueFactory = _ => 1,
            Description = "Number of pages to retrieve for collection commands."
        };
        var allOption = new Option<bool>("--all")
            { Recursive = true, Description = "Retrieve all pages for collection commands." };

        var defs = new OptionDefinitions(
            cookiesOption,
            hlOption,
            glOption,
            authUserOption,
            pageIdOption,
            visitorDataOption,
            rolloutTokenOption,
            poTokenOption,
            formatOption,
            prettyOption,
            continuationOption,
            pagesOption,
            allOption);

        return defs;
    }
}

public sealed record OptionDefinitions(
    Option<string?> Cookies,
    Option<string> Hl,
    Option<string> Gl,
    Option<int> AuthUser,
    Option<string?> PageId,
    Option<string?> VisitorData,
    Option<string?> RolloutToken,
    Option<string?> PoToken,
    Option<string> Format,
    Option<bool> Pretty,
    Option<string?> Continuation,
    Option<int> Pages,
    Option<bool> All)
{
    public void AddToCommand(Command command)
    {
        command.Options.Add(Cookies);
        command.Options.Add(Hl);
        command.Options.Add(Gl);
        command.Options.Add(AuthUser);
        command.Options.Add(PageId);
        command.Options.Add(VisitorData);
        command.Options.Add(RolloutToken);
        command.Options.Add(PoToken);
        command.Options.Add(Format);
        command.Options.Add(Pretty);
        command.Options.Add(Continuation);
        command.Options.Add(Pages);
        command.Options.Add(All);
    }

    public GlobalOptions Parse(ParseResult parseResult)
    {
        var cookies = parseResult.GetValue(Cookies);
        var hl = parseResult.GetValue(Hl) ?? "en";
        var gl = parseResult.GetValue(Gl) ?? "US";
        var authUser = parseResult.GetValue(AuthUser);
        var pageId = parseResult.GetValue(PageId);
        var visitorData = parseResult.GetValue(VisitorData);
        var rolloutToken = parseResult.GetValue(RolloutToken);
        var poToken = parseResult.GetValue(PoToken);
        var format = (parseResult.GetValue(Format) ?? "json").ToLowerInvariant();
        var pretty = parseResult.GetValue(Pretty);
        var continuation = parseResult.GetValue(Continuation);
        var pages = parseResult.GetValue(Pages);
        var all = parseResult.GetValue(All);

        var pagesResult = parseResult.GetResult(Pages);
        var hasExplicitPages = pagesResult is not null && !pagesResult.Implicit;

        var continuationResult = parseResult.GetResult(Continuation);
        var hasExplicitContinuation = continuationResult is not null && !continuationResult.Implicit &&
                                      !string.IsNullOrWhiteSpace(continuation);

        return new GlobalOptions(
            cookies,
            hl,
            gl,
            authUser,
            pageId,
            visitorData,
            rolloutToken,
            poToken,
            format,
            pretty,
            continuation,
            pages,
            all,
            hasExplicitPages,
            hasExplicitContinuation);
    }
}