# AXAML and Avalonia in this project

## What is an `.axaml` file?

`.axaml` is Avalonia's XAML markup file extension. It contains XML-like declarations for the visual tree, controls, layout, styles, resources, and data bindings used by an Avalonia application. It is markup interpreted/compiled by Avalonia; it is not a C# source file and is not the same thing as an `.xaml.cs` code-behind file from WPF.

In this project:

- `src/DevStackStudio.App/App.axaml` defines app-wide theme and styles.
- `src/DevStackStudio.App/Views/MainWindow.axaml` declares the main window and its wizard layout.
- `App.axaml.cs` loads the application markup. `MainWindow.axaml.cs` is the view's code-behind partial class; most behavior is in `MainViewModel.cs`.

The root `xmlns="https://github.com/avaloniaui"` maps Avalonia controls to the default XML namespace. `xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"` provides XAML directives such as `x:Class`. `x:Class` connects markup to its C# partial class. Bindings such as `{Binding ProjectName}` connect UI properties to the view model; commands such as `{Binding GenerateProjectCommand}` invoke view-model actions.

## What Avalonia does here

Avalonia is the cross-platform UI framework for the desktop control plane. It renders the wizard on Windows, macOS, and Linux. It does not generate the backend or frontend; that work is performed by the C# generator projects.

## The Avalonia package references

Versions are centrally pinned in `Directory.Packages.props` (currently `12.1.2`); the app project omits versions because central package management is enabled.

| Reference | Role in this application |
|---|---|
| `Avalonia` | Core Avalonia UI controls, properties, markup, and application framework. |
| `Avalonia.Desktop` | Desktop platform integration and lifetime support used to launch a native desktop window. |
| `Avalonia.Themes.Fluent` | Fluent theme resources and styling; loaded by `App.axaml`. |
| `Avalonia.Fonts.Inter` | Inter font integration; registered during app startup with `.WithInterFont()`. |
| `Avalonia.Controls.DataGrid` | DataGrid control package and Fluent theme resources. The app explicitly includes its Fluent DataGrid theme in `App.axaml`; the current schema editor uses a custom `Grid` with `ItemsControl`, rather than a `DataGrid` control. |

Other app references include CommunityToolkit.Mvvm for observable view models/commands, Microsoft.Extensions.DependencyInjection for service registration, and Serilog for logging. They are not Avalonia packages.

## How to explain the UI structure to a client

`MainWindow.axaml` is the screen description: header/status, left wizard navigation, selected step content, project summary, and footer navigation. The step buttons select one of eight pages: welcome/environment, project details, database, schema designer, backend, frontend, review/generation, and Docker orchestration. `MainViewModel` supplies the state and commands. For example, the screen binds the project name to `ProjectName`, database choices to `DatabaseProviders`, and the generate button to `GenerateProjectCommand`.

This separation keeps layout in markup and workflow/data operations in C#. When changing a control, check both the binding name in `.axaml` and the corresponding public property or command in the view model.

## Small markup example

```xml
<TextBox Text="{Binding ProjectName}" />
<Button Content="Generate" Command="{Binding GenerateProjectCommand}" />
```

The first line displays/edits the bound project name. The second invokes the bound command when clicked. A valid binding depends on the view's data context being set; `App.axaml.cs` creates `MainWindow` with a `MainViewModel` data context.
