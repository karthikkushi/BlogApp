using MudBlazor;

namespace BlogAppFrontend
{
    public static class CustomMudTheme
    {
        public static MudTheme ModernLightTheme = new MudTheme()
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#2563eb",
                Secondary = "#2563eb",
                Background = "#f8fafc",
                AppbarBackground = "#ffffff",
                DrawerBackground = "#f1f5f9",
                Surface = "#ffffff",
                TextPrimary = "#1e293b",
                TextSecondary = "#64748b",
                Success = "#22c55e",
                Error = "#ef4444",
                Warning = "#f59e42",
                Info = "#2563eb"
            },
            Typography = new Typography
            {
                Default = new Default
                {
                    FontFamily = new[] { "Inter", "Roboto", "Open Sans", "Segoe UI", "Arial", "sans-serif" }
                }
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "12px"
            }
        };

        public static MudTheme ModernDarkTheme = new MudTheme()
        {
            PaletteDark = new PaletteDark
            {
                Primary = "#2563eb",
                Secondary = "#2563eb",
                Background = "#181a20",
                AppbarBackground = "#23272f",
                DrawerBackground = "#23272f",
                Surface = "#23272f",
                TextPrimary = "#f1f5f9",
                TextSecondary = "#94a3b8",
                Success = "#22c55e",
                Error = "#ef4444",
                Warning = "#f59e42",
                Info = "#2563eb"
            },
            Typography = new Typography
            {
                Default = new Default
                {
                    FontFamily = new[] { "Inter", "Roboto", "Open Sans", "Segoe UI", "Arial", "sans-serif" }
                }
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "12px"
            }
        };
    }
}
