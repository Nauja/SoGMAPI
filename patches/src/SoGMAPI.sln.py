from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList | None:
    actions = ActionList()

    for a, b in (
        (
            """Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "SoGMAPI.Tests", "SoGMAPI.Tests\SoGMAPI.Tests.csproj", "{AA95884B-7097-476E-92C8-D0500DE9D6D1}"
EndProject""",
            "",
        ),
        (
            """Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "SoGMAPI.Web", "SoGMAPI.Web\SoGMAPI.Web.csproj", "{80EFD92F-728F-41E0-8A5B-9F6F49A91899}"
EndProject""",
            "",
        ),
        (
            """Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "SoGMAPI.Tests.ModApiProvider", "SoGMAPI.Tests.ModApiProvider\SoGMAPI.Tests.ModApiProvider.csproj", "{239AEEAC-07D1-4A3F-AA99-8C74F5038F50}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "SoGMAPI.Tests.ModApiConsumer", "SoGMAPI.Tests.ModApiConsumer\SoGMAPI.Tests.ModApiConsumer.csproj", "{2A4DF030-E8B1-4BBD-AA93-D4DE68CB9D85}"
EndProject""",
            "",
        ),
        (
            """Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "SoGMAPI.ModBuildConfig.Analyzer.Tests", "SoGMAPI.ModBuildConfig.Analyzer.Tests\SoGMAPI.ModBuildConfig.Analyzer.Tests.csproj", "{680B2641-81EA-467C-86A5-0E81CDC57ED0}"
EndProject""",
            "",
        ),
        (
            """Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "SoGMAPI.ModBuildConfig", "SoGMAPI.ModBuildConfig\SoGMAPI.ModBuildConfig.csproj", "{1B3821E6-D030-402C-B3A1-7CA45C2800EA}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "SoGMAPI.ModBuildConfig.Analyzer", "SoGMAPI.ModBuildConfig.Analyzer\SoGMAPI.ModBuildConfig.Analyzer.csproj", "{517677D7-7299-426F-B1A3-47BDCC2F1214}"
EndProject""",
            "",
        ),
        (
            """		{680B2641-81EA-467C-86A5-0E81CDC57ED0}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{680B2641-81EA-467C-86A5-0E81CDC57ED0}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{680B2641-81EA-467C-86A5-0E81CDC57ED0}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{680B2641-81EA-467C-86A5-0E81CDC57ED0}.Release|Any CPU.Build.0 = Release|Any CPU""",
            "",
        ),
        (
            """		{AA95884B-7097-476E-92C8-D0500DE9D6D1}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{AA95884B-7097-476E-92C8-D0500DE9D6D1}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{AA95884B-7097-476E-92C8-D0500DE9D6D1}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{AA95884B-7097-476E-92C8-D0500DE9D6D1}.Release|Any CPU.Build.0 = Release|Any CPU""",
            "",
        ),
        (
            """		{80EFD92F-728F-41E0-8A5B-9F6F49A91899}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{80EFD92F-728F-41E0-8A5B-9F6F49A91899}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{80EFD92F-728F-41E0-8A5B-9F6F49A91899}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{80EFD92F-728F-41E0-8A5B-9F6F49A91899}.Release|Any CPU.Build.0 = Release|Any CPU
		{239AEEAC-07D1-4A3F-AA99-8C74F5038F50}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{239AEEAC-07D1-4A3F-AA99-8C74F5038F50}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{239AEEAC-07D1-4A3F-AA99-8C74F5038F50}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{239AEEAC-07D1-4A3F-AA99-8C74F5038F50}.Release|Any CPU.Build.0 = Release|Any CPU
		{2A4DF030-E8B1-4BBD-AA93-D4DE68CB9D85}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{2A4DF030-E8B1-4BBD-AA93-D4DE68CB9D85}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{2A4DF030-E8B1-4BBD-AA93-D4DE68CB9D85}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{2A4DF030-E8B1-4BBD-AA93-D4DE68CB9D85}.Release|Any CPU.Build.0 = Release|Any CPU""",
            "",
        ),
        (
            """		{1B3821E6-D030-402C-B3A1-7CA45C2800EA}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{1B3821E6-D030-402C-B3A1-7CA45C2800EA}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{1B3821E6-D030-402C-B3A1-7CA45C2800EA}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{1B3821E6-D030-402C-B3A1-7CA45C2800EA}.Release|Any CPU.Build.0 = Release|Any CPU
		{517677D7-7299-426F-B1A3-47BDCC2F1214}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{517677D7-7299-426F-B1A3-47BDCC2F1214}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{517677D7-7299-426F-B1A3-47BDCC2F1214}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{517677D7-7299-426F-B1A3-47BDCC2F1214}.Release|Any CPU.Build.0 = Release|Any CPU""",
            "",
        ),
        (
            """		SoGMAPI.Internal\SoGMAPI.Internal.projitems*{80efd92f-728f-41e0-8a5b-9f6f49a91899}*SharedItemsImports = 5""",
            "",
        ),
        (
            """		{680B2641-81EA-467C-86A5-0E81CDC57ED0} = {82D22ED7-A0A7-4D64-8E92-4B6A5E74ED11}""",
            "",
        ),
        (
            """		{AA95884B-7097-476E-92C8-D0500DE9D6D1} = {82D22ED7-A0A7-4D64-8E92-4B6A5E74ED11}""",
            "",
        ),
        (
            """		{239AEEAC-07D1-4A3F-AA99-8C74F5038F50} = {82D22ED7-A0A7-4D64-8E92-4B6A5E74ED11}
		{2A4DF030-E8B1-4BBD-AA93-D4DE68CB9D85} = {82D22ED7-A0A7-4D64-8E92-4B6A5E74ED11}""",
            "",
        ),
    ):
        actions.add(ReplaceText("", a, b))

    return actions
