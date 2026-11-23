# Weather Intelligence Dashboard

A .NET 8 console application that consumes Open-Meteo REST APIs for current conditions, forecasts, location search, city comparison, historical weather, and saved locations.

## Features

- Favorite location dashboard
- Current conditions
- 7-day forecast
- Hourly forecast
- Location search using Open-Meteo geocoding
- Side-by-side weather comparison
- Historical weather search
- Historical summary statistics
- Saved locations
- Changeable favorite location
- JSON settings persistence
- In-memory API response caching
- Timeout and HTTP error handling
- Colorblind-friendly console UI

## Accessibility

The console theme uses a small, high-contrast palette:
- Cyan: titles and table headers
- Yellow: favorite/highlighted items
- White: normal data
- Gray: secondary text

Color is never the only indicator. Headings, labels, symbols, and text always carry the same meaning.

## APIs

The app uses:
- Open-Meteo Forecast API
- Open-Meteo Geocoding API
- Open-Meteo Historical Weather API

No API key is required for normal non-commercial/open-source use.

## Units

The app is configured for:
- Fahrenheit
- miles per hour
- inches of precipitation

## Run

Open `WeatherIntelligenceDashboard.csproj` in Visual Studio 2022 with .NET 8 installed, then run the project.

No external NuGet packages are required.
