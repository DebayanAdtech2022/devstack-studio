using System.Text;

using System.Text.Json;

using DevStackStudio.Core.Contracts;

using DevStackStudio.Core.Models;

using DevStackStudio.Core.Naming;

using DevStackStudio.Core.Providers;



namespace DevStackStudio.Generators.Frontend;



public sealed class NextJsFrontendGenerator

{

    public IReadOnlyList<GeneratedFile> Generate(ProjectConfiguration configuration)

    {

        if (configuration.Frontend != FrontendFramework.NextJs)

        {

            throw new InvalidOperationException("React + Vite is registered but not implemented.");

        }



        var files = new List<GeneratedFile>

        {

            File("frontend/package.json", PackageJson(configuration.ProjectName)),

            File("frontend/tsconfig.json", TsConfig()),

            File("frontend/next.config.ts", "import type { NextConfig } from 'next';\nconst config: NextConfig = { output: 'standalone' };\nexport default config;\n"),

            File("frontend/next-env.d.ts", "/// <reference types=\"next\" />\n/// <reference types=\"next/image-types/global\" />\n"),

            File("frontend/app/globals.css", Css()),

            File("frontend/components/Sidebar.tsx", Sidebar(configuration)),

            File("frontend/components/Header.tsx", Header()),

            File("frontend/app/layout.tsx", Layout(configuration)),

            File("frontend/app/page.tsx", Home(configuration)),

            File("frontend/public/.gitkeep", ""),

            File("frontend/lib/env.ts", Env()),

            File("frontend/lib/api.ts", Api(configuration)),

            File("frontend/components/Status.tsx", Status()),

            File("frontend/Dockerfile", Dockerfile()),

            File("frontend/.dockerignore", "node_modules\n.next\n"),

            File("frontend/vitest.config.ts", "import { defineConfig } from 'vitest/config';\nexport default defineConfig({ test: { environment: 'node' } });\n"),

            File("frontend/lib/api.test.ts", "import { describe, it, expect } from 'vitest';\nimport { joinUrl } from './api';\nit('joins urls', () => { expect(joinUrl('http://api', '/health')).toBe('http://api/health'); });\n")

        };



        // Industrial DT-aligned Frontend Architecture

        files.Add(File("frontend/src/AxiosInstance.ts", AxiosInstanceFile()));

        files.Add(File("frontend/src/state/ErrorState.tsx", ErrorStateFile()));

        files.Add(File("frontend/src/types/ApiError.ts", ApiErrorFile()));

        files.Add(File("frontend/src/types/HttpStatus.ts", HttpStatusFile()));

        files.Add(File("frontend/src/types/PagedResponse.tsx", PagedResponseFile()));

        files.Add(File("frontend/src/hooks/useGet.tsx", UseGetHookFile()));

        files.Add(File("frontend/src/hooks/usePost.tsx", UsePostHookFile()));

        files.Add(File("frontend/src/hooks/usePut.tsx", UsePutHookFile()));

        files.Add(File("frontend/src/hooks/useDelete.tsx", UseDeleteHookFile()));



        foreach (var entity in configuration.Entities)

        {

            var slug = NameFactory.ToKebabCase(NameFactory.Pluralize(entity.Name));

            files.Add(File($"frontend/app/{slug}/page.tsx", ListPage(entity, configuration)));

            files.Add(File($"frontend/app/{slug}/new/page.tsx", FormPage(entity, configuration, isNew: true)));

            files.Add(File($"frontend/app/{slug}/[id]/page.tsx", FormPage(entity, configuration, isNew: false)));



            // Industrial DT-aligned types and constants per entity

            files.Add(File($"frontend/src/constants/{entity.Name}ApiConstants.ts", EntityApiConstantsFile(entity)));

            files.Add(File($"frontend/src/types/I{entity.Name}.ts", EntityTypeFile(entity)));

            files.Add(File($"frontend/src/types/CreateTypes/ICreate{entity.Name}.ts", EntityCreateTypeFile(entity)));

        }



        return files;

    }



    private static GeneratedFile File(string path, string contents) => new() { RelativePath = path, Contents = contents };



    private static string PackageJson(string name) => JsonSerializer.Serialize(new Dictionary<string, object>

    {

        ["name"] = NameFactory.ToKebabCase(name) + "-web",

        ["private"] = true,

        ["version"] = "0.1.0",

        ["scripts"] = new Dictionary<string, string>

        {

            ["dev"] = "next dev -p 3000",

            ["build"] = "next build",

            ["start"] = "next start -p 3000",

            ["lint"] = "next lint",

            ["test"] = "vitest run",

            ["typecheck"] = "tsc --noEmit"

        },

        ["dependencies"] = new Dictionary<string, string>

        {

            ["next"] = "15.5.4",

            ["react"] = "19.1.1",

            ["react-dom"] = "19.1.1",

            ["axios"] = "^1.7.9"

        },

        ["devDependencies"] = new Dictionary<string, string>

        {

            ["typescript"] = "5.9.2",

            ["@types/node"] = "24.5.2",

            ["@types/react"] = "19.1.13",

            ["@types/react-dom"] = "19.1.9",

            ["vitest"] = "3.2.4"

        }

    }, new JsonSerializerOptions { WriteIndented = true });



    private static string TsConfig() => """

        {

          "compilerOptions": {

            "target": "ES2022",

            "lib": ["dom", "dom.iterable", "es2022"],

            "allowJs": false,

            "skipLibCheck": true,

            "strict": true,

            "noEmit": true,

            "esModuleInterop": true,

            "module": "esnext",

            "moduleResolution": "bundler",

            "resolveJsonModule": true,

            "isolatedModules": true,

            "jsx": "preserve",

            "incremental": true,

            "plugins": [{ "name": "next" }],

            "paths": { "@/*": ["./src/*", "./*"] }

          },

          "include": ["next-env.d.ts", "**/*.ts", "**/*.tsx"],

          "exclude": ["node_modules"]

        }

        """;



        private static string Env() => """

        export function apiBaseUrl(): string {

          const value = process.env.NEXT_PUBLIC_API_URL;

          if (!value) {

            throw new Error('NEXT_PUBLIC_API_URL is required. Use a browser-reachable URL, not a Docker service hostname.');

          }

          return value.replace(/\/$/, '');

        }

        """;



    private static string Api(ProjectConfiguration configuration)

    {

        var sb = new StringBuilder();

        sb.AppendLine("import { apiBaseUrl } from './env';");

        sb.AppendLine();

        sb.AppendLine("export function joinUrl(base: string, path: string): string {");

        sb.AppendLine("  return `${base.replace(/\\/$/, '')}/${path.replace(/^\\//, '')}`;");

        sb.AppendLine("}");

        sb.AppendLine();

        sb.AppendLine("async function request<T>(path: string, init?: RequestInit): Promise<T> {");

        sb.AppendLine("  const response = await fetch(joinUrl(apiBaseUrl(), path), {");

        sb.AppendLine("    ...init,");

        sb.AppendLine("    headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) }");

        sb.AppendLine("  });");

        sb.AppendLine("  if (!response.ok) {");

        sb.AppendLine("    let err = `Request failed (${response.status})`;");

        sb.AppendLine("    try { const eb = await response.json(); err = eb.error ?? eb.title ?? eb.message ?? JSON.stringify(eb); } catch {}");

        sb.AppendLine("    throw new Error(err);");

        sb.AppendLine("  }");

        sb.AppendLine("  const body = await response.json();");

        sb.AppendLine("  return body as T;");

        sb.AppendLine("}");

        sb.AppendLine();

        sb.AppendLine("export type Paged<T> = { Data?: T[]; items?: T[]; Page?: number; page?: number; Total?: number; total?: number; totalCount?: number; TotalPages?: number; totalPages?: number; };");

        sb.AppendLine("export function getPagedItems<T>(paged: any): T[] { if (!paged) return []; if (Array.isArray(paged)) return paged; return paged.Data || paged.data || paged.items || []; }");

        sb.AppendLine("export function getPagedTotal(paged: any): number { if (!paged) return 0; if (Array.isArray(paged)) return paged.length; return paged.Total ?? paged.total ?? paged.totalCount ?? 0; }");

        sb.AppendLine("export function getPagedTotalPages(paged: any, pageSize = 10): number { if (!paged) return 1; const tp = paged.TotalPages ?? paged.totalPages; if (typeof tp === 'number' && tp > 0) return tp; const t = getPagedTotal(paged); return Math.max(1, Math.ceil(t / pageSize)); }");

        foreach (var entity in configuration.Entities)

        {

            var p = NameFactory.Pluralize(entity.Name);

            var slug = NameFactory.ToKebabCase(p);

            var fields = string.Join("; ", entity.Columns.Select(c =>
            {
                var camel = NameFactory.ToCamelCase(c.Name);
                var type = TsType(c);
                return string.Equals(camel, c.Name, StringComparison.Ordinal)
                    ? $"{camel}?: {type}"
                    : $"{camel}?: {type}; {c.Name}?: {type}";
            }));

            sb.AppendLine($"export type {entity.Name} = {{ {fields} }};");

            sb.AppendLine($"export const {NameFactory.ToCamelCase(p)}Api = {{");

            sb.AppendLine($"  list: (page = 1, search = '') => request<Paged<{entity.Name}>>(`/api/{slug}?page=${{page}}&search=${{encodeURIComponent(search)}}`),");

            sb.AppendLine($"  get: (id: string) => request<{entity.Name}>(`/api/{slug}/${{id}}`),");

            sb.AppendLine($"  create: (body: Partial<{entity.Name}>) => request<string>(`/api/{slug}`, {{ method: 'POST', body: JSON.stringify(body) }}),");

            sb.AppendLine($"  update: (id: string, body: Partial<{entity.Name}>) => request<boolean>(`/api/{slug}/${{id}}`, {{ method: 'PUT', body: JSON.stringify(body) }}),");

            sb.AppendLine($"  remove: (id: string) => request<boolean>(`/api/{slug}/${{id}}`, {{ method: 'DELETE' }})");

            sb.AppendLine("};");

        }



        return sb.ToString();

    }



    private static string TsType(ColumnDefinition column) => column.LogicalType switch

    {

        "int" or "bigint" or "decimal" => "number",

        "boolean" => "boolean",

        _ => "string"

    };



    private static string Css() => """

        :root {

          color-scheme: light;

          --bg-app: #f4f6f9;

          --bg-card: #ffffff;

          --border-color: #e2e8f0;

          --border-subtle: #f1f5f9;

          --text-main: #1e293b;

          --text-muted: #64748b;

          --primary-blue: #0f52ba;

          --primary-hover: #0d47a1;

          --primary-subtle: #eff6ff;

          --danger: #ef4444;

          --danger-bg: #fef2f2;

        }



        * {

          box-sizing: border-box;

          margin: 0;

          padding: 0;

        }



        body {

          margin: 0;

          background: var(--bg-app);

          color: var(--text-main);

          font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;

          line-height: 1.5;

          -webkit-font-smoothing: antialiased;

        }



        .app-layout {

          display: flex;

          min-height: 100vh;

        }



        .app-sidebar {

          width: 240px;

          background: #ffffff;

          border-right: 1px solid var(--border-color);

          display: flex;

          flex-direction: column;

          flex-shrink: 0;

        }



        .sidebar-brand {

          height: 64px;

          padding: 0 1.5rem;

          display: flex;

          align-items: center;

          gap: 0.75rem;

          border-bottom: 1px solid var(--border-subtle);

        }



        .brand-logo {

          width: 36px;

          height: 36px;

          border-radius: 50%;

          background: #0f2b5c;

          color: #ffffff;

          display: flex;

          align-items: center;

          justify-content: center;

        }



        .brand-name {

          font-size: 1.05rem;

          font-weight: 700;

          color: #0f2b5c;

          letter-spacing: -0.01em;

        }



        .sidebar-nav {

          padding: 1rem 0;

          display: flex;

          flex-direction: column;

          gap: 0.25rem;

        }



        .nav-item {

          padding: 0 0.75rem;

        }



        .nav-link {

          display: flex;

          align-items: center;

          gap: 0.75rem;

          padding: 0.65rem 1rem;

          color: var(--text-muted);

          font-size: 0.9rem;

          font-weight: 500;

          text-decoration: none;

          border-radius: 0.375rem;

          transition: all 0.15s;

        }



        .nav-link:hover {

          background: #f8fafc;

          color: var(--primary-blue);

        }



        .nav-link.active {

          background: var(--primary-subtle);

          color: var(--primary-blue);

          font-weight: 600;

        }



        .nav-section {

          padding: 0.5rem 0.75rem 0 0.75rem;

        }



        .nav-section-header {

          display: flex;

          align-items: center;

          justify-content: space-between;

          padding: 0.65rem 1rem;

          color: var(--text-main);

          font-size: 0.9rem;

          font-weight: 600;

          cursor: pointer;

          border-radius: 0.375rem;

          user-select: none;

        }



        .nav-section-header:hover {

          background: #f8fafc;

        }



        .nav-section-title {

          display: flex;

          align-items: center;

          gap: 0.75rem;

        }



        .chevron {

          transition: transform 0.2s;

          color: var(--text-muted);

        }



        .chevron.open {

          transform: rotate(180deg);

        }



        .subnav-list {

          padding: 0.25rem 0 0.5rem 1.25rem;

          display: flex;

          flex-direction: column;

          gap: 0.15rem;

        }



        .subnav-link {

          display: block;

          padding: 0.45rem 1rem;

          font-size: 0.85rem;

          color: var(--text-muted);

          text-decoration: none;

          border-left: 2px solid transparent;

          transition: all 0.15s;

        }



        .subnav-link:hover {

          color: var(--primary-blue);

        }



        .subnav-link.active {

          color: var(--primary-blue);

          font-weight: 600;

          border-left-color: var(--primary-blue);

          background: var(--primary-subtle);

        }



        .app-content-wrapper {

          flex: 1;

          display: flex;

          flex-direction: column;

          min-width: 0;

        }



        .app-header {

          height: 64px;

          background: #ffffff;

          border-bottom: 1px solid var(--border-color);

          padding: 0 2rem;

          display: flex;

          align-items: center;

          justify-content: space-between;

        }



        .header-pill {

          background: #f1f5f9;

          color: #334155;

          font-size: 0.85rem;

          font-weight: 600;

          padding: 0.35rem 0.85rem;

          border-radius: 9999px;

          display: inline-flex;

          align-items: center;

        }



        .header-right {

          display: flex;

          align-items: center;

          gap: 1.5rem;

        }



        .header-icon-btn {

          background: transparent;

          border: none;

          color: var(--text-muted);

          cursor: pointer;

          position: relative;

          display: flex;

          align-items: center;

          justify-content: center;

          padding: 0.35rem;

          border-radius: 50%;

        }



        .header-icon-btn:hover {

          background: #f8fafc;

          color: var(--text-main);

        }



        .notification-dot {

          position: absolute;

          top: 4px;

          right: 5px;

          width: 7px;

          height: 7px;

          background: #ef4444;

          border-radius: 50%;

          border: 1px solid #ffffff;

        }



        .user-profile {

          display: flex;

          align-items: center;

          gap: 0.65rem;

        }



        .user-info {

          display: flex;

          flex-direction: column;

          align-items: flex-end;

          line-height: 1.2;

        }



        .user-name {

          font-size: 0.85rem;

          font-weight: 600;

          color: var(--text-main);

        }



        .user-role {

          font-size: 0.72rem;

          color: var(--text-muted);

        }



        .user-avatar {

          width: 34px;

          height: 34px;

          border-radius: 50%;

          background: #e2e8f0;

          color: #64748b;

          display: flex;

          align-items: center;

          justify-content: center;

        }



        .app-main {

          flex: 1;

          padding: 2rem;

        }



        .card {

          background: #ffffff;

          border: 1px solid var(--border-color);

          border-radius: 0.5rem;

          box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.04);

          padding: 1.75rem;

        }



        .list-toolbar {

          display: flex;

          justify-content: space-between;

          align-items: center;

          margin-bottom: 1.5rem;

          gap: 1rem;

        }



        .search-container {

          position: relative;

          width: 320px;

        }



        .search-icon {

          position: absolute;

          left: 0.85rem;

          top: 50%;

          transform: translateY(-50%);

          color: #94a3b8;

          pointer-events: none;

        }



        .search-input {

          width: 100%;

          padding: 0.55rem 0.875rem 0.55rem 2.4rem;

          font-size: 0.875rem;

          background: #ffffff;

          border: 1px solid var(--border-color);

          border-radius: 0.375rem;

          color: var(--text-main);

          outline: none;

          transition: all 0.15s;

        }



        .search-input:focus {

          border-color: var(--primary-blue);

          box-shadow: 0 0 0 2px rgba(15, 82, 186, 0.12);

        }



        .btn-primary {

          background: var(--primary-blue);

          color: #ffffff;

          font-size: 0.875rem;

          font-weight: 600;

          padding: 0.6rem 1.35rem;

          border-radius: 0.375rem;

          border: none;

          cursor: pointer;

          display: inline-flex;

          align-items: center;

          gap: 0.5rem;

          text-decoration: none;

          transition: background 0.15s;

          box-shadow: 0 1px 2px rgba(15, 82, 186, 0.2);

        }



        .btn-primary:hover {

          background: var(--primary-hover);

        }



        .btn-primary:disabled {

          opacity: 0.6;

          cursor: not-allowed;

        }



        .dt-table {

          width: 100%;

          border-collapse: collapse;

          text-align: left;

          font-size: 0.875rem;

        }



        .dt-table th {

          padding: 0.875rem 1.25rem;

          font-size: 0.8125rem;

          font-weight: 700;

          color: var(--text-main);

          border-bottom: 1px solid var(--border-color);

          background: #ffffff;

        }



        .dt-table th .sort-icon {

          display: inline-block;

          margin-left: 0.35rem;

          color: var(--text-muted);

          font-size: 0.8rem;

        }



        .dt-table td {

          padding: 0.875rem 1.25rem;

          border-bottom: 1px solid var(--border-subtle);

          color: #334155;

        }



        .dt-table tbody tr:nth-child(even) {

          background-color: rgba(188, 188, 188, 0.1);

        }



        .dt-table tbody tr:hover {

          background-color: rgba(15, 82, 186, 0.04);

        }



        .text-right {

          text-align: right;

        }



        .edit-icon-link {

          color: var(--text-muted);

          display: inline-flex;

          align-items: center;

          justify-content: center;

          padding: 0.25rem;

          border-radius: 0.25rem;

          transition: color 0.15s;

        }



        .edit-icon-link:hover {

          color: var(--primary-blue);

        }



        .pagination-bar {

          display: flex;

          align-items: center;

          gap: 0.5rem;

          font-size: 0.875rem;

          color: var(--text-muted);

          padding-top: 1.25rem;

        }



        .pg-btn {

          border: 1px solid #cbd5e1;

          background: #ffffff;

          border-radius: 0.25rem;

          width: 28px;

          height: 28px;

          display: inline-flex;

          align-items: center;

          justify-content: center;

          cursor: pointer;

          color: #475569;

          font-weight: 600;

          transition: all 0.15s;

        }



        .pg-btn:hover:not(:disabled) {

          border-color: var(--primary-blue);

          color: var(--primary-blue);

        }



        .pg-btn:disabled {

          opacity: 0.4;

          cursor: not-allowed;

        }



        .pg-box {

          border: 1px solid #cbd5e1;

          background: #ffffff;

          border-radius: 0.25rem;

          padding: 0.15rem 0.65rem;

          font-weight: 600;

          color: var(--text-main);

          font-size: 0.85rem;

        }



        .pg-all {

          color: var(--text-muted);

          font-weight: 500;

          cursor: pointer;

          margin-left: 0.5rem;

          text-decoration: underline;

        }



        .pg-all:hover {

          color: var(--primary-blue);

        }



        form {

          display: flex;

          flex-direction: column;

          gap: 1.25rem;

          max-width: 600px;

        }



        .form-group {

          display: flex;

          flex-direction: column;

          gap: 0.375rem;

        }



        label {

          font-size: 0.8125rem;

          font-weight: 600;

          color: #334155;

        }



        input, select {

          width: 100%;

          background: #ffffff;

          color: var(--text-main);

          border: 1px solid #cbd5e1;

          border-radius: 0.375rem;

          padding: 0.625rem 0.875rem;

          font-size: 0.9rem;

          outline: none;

          transition: border-color 0.15s;

        }



        input:focus, select:focus {

          border-color: var(--primary-blue);

          box-shadow: 0 0 0 2px rgba(15, 82, 186, 0.15);

        }



        .form-actions {

          display: flex;

          align-items: center;

          gap: 0.75rem;

          margin-top: 0.5rem;

        }



        .btn-cancel {

          display: inline-flex;

          align-items: center;

          padding: 0.6rem 1.25rem;

          font-size: 0.875rem;

          font-weight: 500;

          color: var(--text-muted);

          background: transparent;

          border: 1px solid #cbd5e1;

          border-radius: 0.375rem;

          text-decoration: none;

          transition: all 0.15s;

        }



        .btn-cancel:hover {

          background: #f8fafc;

          color: var(--text-main);

        }



        .error {

          background: var(--danger-bg);

          color: var(--danger);

          border: 1px solid #fecaca;

          border-radius: 0.375rem;

          padding: 0.75rem 1rem;

          font-size: 0.875rem;

          margin-bottom: 1rem;

        }



        .muted {

          color: var(--text-muted);

          font-size: 0.875rem;

        }



        .back-link {

          display: inline-flex;

          align-items: center;

          gap: 0.35rem;

          color: var(--primary-blue);

          text-decoration: none;

          font-size: 0.875rem;

          font-weight: 500;

          margin-bottom: 1.25rem;

        }



        .back-link:hover {

          text-decoration: underline;

        }

        """;

private static string Layout(ProjectConfiguration configuration) => $$"""

        import './globals.css';

        import type { ReactNode } from 'react';

        import { Sidebar } from '@/components/Sidebar';

        import { Header } from '@/components/Header';



        export const metadata = { title: '{{configuration.ProjectName}}' };



        export default function RootLayout({ children }: { children: ReactNode }) {

          return (

            <html lang="en">

              <body>

                <div className="app-layout">

                  <Sidebar />

                  <div className="app-content-wrapper">

                    <Header />

                    <main className="app-main">

                      {children}

                    </main>

                  </div>

                </div>

              </body>

            </html>

          );

        }

        """;



    private static string Sidebar(ProjectConfiguration configuration)

    {

        var masterDataLinks = string.Join(",\n", configuration.Entities.Select(e =>

        {

            var p = NameFactory.Pluralize(e.Name);

            var slug = NameFactory.ToKebabCase(p);

            return $"      {{ name: '{p}', slug: '{slug}' }}";

        }));



        return $$"""

            'use client';

            import Link from 'next/link';

            import { usePathname } from 'next/navigation';

            import { useState } from 'react';



            const masterDataItems = [

            {{masterDataLinks}}

            ];



            export function Sidebar() {

              const pathname = usePathname();

              const [masterDataOpen, setMasterDataOpen] = useState(true);



              return (

                <aside className="app-sidebar">

                  <div className="sidebar-brand">

                    <div className="brand-logo">

                      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">

                        <circle cx="12" cy="12" r="10"/>

                        <circle cx="12" cy="12" r="4"/>

                        <line x1="4.93" y1="4.93" x2="9.17" y2="9.17"/>

                        <line x1="14.83" y1="14.83" x2="19.07" y2="19.07"/>

                        <line x1="14.83" y1="9.17" x2="19.07" y2="4.93"/>

                        <line x1="4.93" y1="19.07" x2="9.17" y2="14.83"/>

                      </svg>

                    </div>

                    <span className="brand-name">{{configuration.ProjectName}}</span>

                  </div>



                  <nav className="sidebar-nav">

                    <div className="nav-section">

                      <div className="nav-section-header" onClick={() => setMasterDataOpen(!masterDataOpen)}>

                        <div className="nav-section-title">

                          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">

                            <ellipse cx="12" cy="5" rx="9" ry="3"/>

                            <path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/>

                            <path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/>

                          </svg>

                          <span>Master Data</span>

                        </div>

                        <svg className={`chevron ${masterDataOpen ? 'open' : ''}`} width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">

                          <polyline points="6 9 12 15 18 9"/>

                        </svg>

                      </div>



                      {masterDataOpen && (

                        <div className="subnav-list">

                          {masterDataItems.map(item => {

                            const isActive = pathname.startsWith(`/${item.slug}`);

                            return (

                              <Link

                                key={item.slug}

                                href={`/${item.slug}`}

                                className={`subnav-link ${isActive ? 'active' : ''}`}

                              >

                                {item.name}

                              </Link>

                            );

                          })}

                        </div>

                      )}

                    </div>

                  </nav>

                </aside>

              );

            }

            """;

    }



    private static string Header() => """

        'use client';

        import { usePathname } from 'next/navigation';



        export function Header() {

          const pathname = usePathname();

          const segment = pathname.split('/').filter(Boolean)[0] || 'Home';

          const title = segment.charAt(0).toUpperCase() + segment.slice(1);



          return (

            <header className="app-header">

              <div className="header-left">

                <span className="header-pill">{title} Master Data</span>

              </div>

              <div className="header-right">

                <button className="header-icon-btn" title="Notifications" type="button">

                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">

                    <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>

                    <path d="M13.73 21a2 2 0 0 1-3.46 0"/>

                  </svg>

                  <span className="notification-dot"></span>

                </button>



                <div className="user-profile">

                  <div className="user-info">

                    <span className="user-name">User Name</span>

                  </div>

                  <div className="user-avatar">

                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">

                      <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>

                      <circle cx="12" cy="7" r="4"/>

                    </svg>

                  </div>

                </div>

              </div>

            </header>

          );

        }

        """;



    private static string Home(ProjectConfiguration configuration)

    {

        var first = configuration.Entities.FirstOrDefault();

        var firstSlug = first != null ? NameFactory.ToKebabCase(NameFactory.Pluralize(first.Name)) : "";

        return $$"""

            import { redirect } from 'next/navigation';



            export default function HomePage() {

              redirect('/{{firstSlug}}');

            }

            """;

    }

private static string Status() => """

        import type { ReactNode } from 'react';

        export function Status(props: { loading?: boolean; error?: string; empty?: boolean; children: ReactNode }) {

          if (props.loading) return <p className="muted">Loading…</p>;

          if (props.error) return <p className="error">{props.error}</p>;

          if (props.empty) return <p className="muted">No records yet.</p>;

          return <>{props.children}</>;

        }

        """;



    private static string ListPage(EntityDefinition entity, ProjectConfiguration configuration)

    {

        var p = NameFactory.Pluralize(entity.Name);

        var slug = NameFactory.ToKebabCase(p);

        var api = NameFactory.ToCamelCase(p) + "Api";

        var nameCol = entity.Columns.FirstOrDefault(c => c.LogicalType is "string" or "text") ?? entity.Columns[0];

        var pk = entity.Columns.FirstOrDefault(c => c.IsPrimaryKey) ?? entity.Columns.First();

        return $$"""

            'use client';

            import { useEffect, useState, useMemo } from 'react';

            import Link from 'next/link';

            import { {{api}}, type {{entity.Name}}, type Paged, getPagedItems, getPagedTotal, getPagedTotalPages } from '@/lib/api';

            import { Status } from '@/components/Status';



            export default function {{p}}Page() {

              const [data, setData] = useState<Paged<{{entity.Name}}> | null>(null);

              const [error, setError] = useState<string>();

              const [loading, setLoading] = useState(true);

              const [search, setSearch] = useState('');

              const [currentPage, setCurrentPage] = useState(1);

              const [allMode, setAllMode] = useState(false);



              useEffect(() => {

                setLoading(true);

                setError(undefined);

                {{api}}.list(allMode ? 1 : currentPage, search)

                  .then(setData)

                  .catch((e: Error) => setError(e.message))

                  .finally(() => setLoading(false));

              }, [currentPage, search, allMode]);



              const items = getPagedItems<{{entity.Name}}>(data);

              const total = getPagedTotal(data);

              const totalPages = allMode ? 1 : getPagedTotalPages(data, 10);



              const onPageChange = (newPage: number) => {

                if (allMode) setAllMode(false);

                const clamped = Math.max(1, Math.min(newPage, totalPages));

                setCurrentPage(clamped);

              };



              const onSearchChange = (val: string) => {

                setSearch(val);

                setCurrentPage(1);

              };



              const onToggleAll = () => {

                if (allMode) {

                  setAllMode(false);

                  setCurrentPage(1);

                } else {

                  setAllMode(true);

                  setCurrentPage(1);

                }

              };



              return (

                <section className="card">

                  <div className="list-toolbar">

                    <div className="search-container">

                      <svg className="search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">

                        <circle cx="11" cy="11" r="8"/>

                        <line x1="21" y1="21" x2="16.65" y2="16.65"/>

                      </svg>

                      <input

                        type="text"

                        className="search-input"

                        placeholder="Search"

                        value={search}

                        onChange={(e) => onSearchChange(e.target.value)}

                      />

                    </div>

                    <Link href="/{{slug}}/new" className="btn-primary">

                      + Add {{entity.Name}}

                    </Link>

                  </div>



                  <Status loading={loading} error={error} empty={!loading && items.length === 0}>

                    <table className="dt-table">

                      <thead>

                        <tr>

                          <th>

                            {{nameCol.Name}} <span className="sort-icon">⇅</span>

                          </th>

                          <th className="text-right">Actions</th>

                        </tr>

                      </thead>

                      <tbody>

                        {items.map((row: any) => {

                          const rowId = row.{{NameFactory.ToCamelCase(pk.Name)}} ?? row.{{pk.Name}};

                          const rowName = row.{{NameFactory.ToCamelCase(nameCol.Name)}} ?? row.{{nameCol.Name}};

                          return (

                            <tr key={String(rowId)}>

                              <td>{String(rowName ?? '')}</td>

                              <td className="text-right">

                                <Link href={`/{{slug}}/${rowId}`} className="edit-icon-link" title="Edit">

                                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">

                                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>

                                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>

                                  </svg>

                                </Link>

                              </td>

                            </tr>

                          );

                        })}

                      </tbody>

                    </table>



                    <div className="pagination-bar">

                      <span>Move to page :</span>

                      <button className="pg-btn" disabled={allMode || currentPage <= 1} onClick={() => onPageChange(currentPage - 1)}>&lt;</button>

                      <span className="pg-box">{allMode ? 1 : currentPage}</span>

                      <span>of {totalPages}</span>

                      <button className="pg-btn" disabled={allMode || currentPage >= totalPages} onClick={() => onPageChange(currentPage + 1)}>&gt;</button>

                      <span className="pg-all" onClick={onToggleAll}>{allMode ? 'Paginate' : 'All'}</span>

                    </div>

                  </Status>

                </section>

              );

            }

            """;

    }



    private static string FormPage(EntityDefinition entity, ProjectConfiguration configuration, bool isNew)

    {

        var p = NameFactory.Pluralize(entity.Name);

        var slug = NameFactory.ToKebabCase(p);

        var api = NameFactory.ToCamelCase(p) + "Api";

        var writable = entity.Columns.Where(c => !c.IsPrimaryKey || c.Identity == IdentityStrategy.None).ToList();

        var pk = entity.Columns.FirstOrDefault(c => c.IsPrimaryKey) ?? entity.Columns.First();



        var fkColumns = writable.Where(c => c.ForeignKeyTargetEntityId is not null).ToList();

        var fkEntityImports = new StringBuilder();

        var fkEffects = new StringBuilder();



        foreach (var fk in fkColumns)

        {

            var targetEntity = configuration.Entities.FirstOrDefault(e => e.Id == fk.ForeignKeyTargetEntityId);

            if (targetEntity != null)

            {

                var targetPlural = NameFactory.Pluralize(targetEntity.Name);

                var targetApi = NameFactory.ToCamelCase(targetPlural) + "Api";

                fkEntityImports.AppendLine($"import {{ {targetApi}, type {targetEntity.Name} }} from '@/lib/api';");

                fkEffects.AppendLine($$"""

                  const [{{NameFactory.ToCamelCase(targetPlural)}}, set{{targetPlural}}] = useState<{{targetEntity.Name}}[]>([]);

                  useEffect(() => {

                    {{targetApi}}.list(1, '').then(res => set{{targetPlural}}(getPagedItems(res))).catch(() => {});

                  }, []);

                """);

            }

        }



        var fields = writable.Count == 0

            ? "<p className=\"text-muted\">No editable fields defined for this entity.</p>"

            : string.Join("\n", writable.Select(c =>

        {

            var camelName = NameFactory.ToCamelCase(c.Name);

            if (c.ForeignKeyTargetEntityId is not null)

            {

                var target = configuration.Entities.FirstOrDefault(e => e.Id == c.ForeignKeyTargetEntityId);

                if (target != null)

                {

                    var targetPlural = NameFactory.Pluralize(target.Name);

                    var targetPk = target.Columns.FirstOrDefault(col => col.IsPrimaryKey) ?? target.Columns.First();

                    var targetNameCol = target.Columns.FirstOrDefault(col => col.LogicalType is "string" or "text") ?? targetPk;

                    var selectValueAttr = isNew

                        ? "defaultValue=\"\""

                        : $$"""value={String(formData['{{camelName}}'] ?? formData['{{c.Name}}'] ?? formData['{{c.Name.ToLowerInvariant()}}'] ?? '')} onChange={handleChange}""";



                    return $$"""

                              <div className="form-group">

                                <label>{{c.Name}}</label>

                                <select name="{{camelName}}" {{selectValueAttr}}>

                                  <option value="" disabled>Select {{target.Name}}...</option>

                                  {{{NameFactory.ToCamelCase(targetPlural)}}.map((item: any) => {

                                    const itemId = item.{{NameFactory.ToCamelCase(targetPk.Name)}} ?? item.{{targetPk.Name}};

                                    const itemLabel = item.{{NameFactory.ToCamelCase(targetNameCol.Name)}} ?? item.{{targetNameCol.Name}} ?? itemId;

                                    return (

                                      <option key={String(itemId)} value={String(itemId)}>

                                        {String(itemLabel)}

                                      </option>

                                    );

                                  })}

                                </select>

                              </div>

                    """;

                }

            }



            var inputType = c.LogicalType switch

            {

                "int" or "bigint" or "decimal" => "number",

                "date" => "date",

                "timestamp" => "datetime-local",

                _ => "text"

            };



            var stepAttr = c.LogicalType == "decimal" ? " step=\"any\"" : "";

            var inputAttrs = isNew

                ? $"type=\"{inputType}\"{stepAttr} name=\"{camelName}\" placeholder=\"{c.Name}\""

                : $$"""type="{{inputType}}"{{stepAttr}} name="{{camelName}}" placeholder="{{c.Name}}" value={formData['{{camelName}}'] ?? formData['{{c.Name}}'] ?? formData['{{c.Name.ToLowerInvariant()}}'] ?? ''} onChange={handleChange}""";



            return $$"""

                      <div className="form-group">

                        <label>{{c.Name}}</label>

                        <input {{inputAttrs}} />

                      </div>

            """;

        }));



        var typeConversions = string.Join("\n", writable.Select(c =>

        {

            var camelName = NameFactory.ToCamelCase(c.Name);

            if (c.LogicalType is "int" or "bigint")

            {

                return $"    if (payload.{camelName} !== undefined && payload.{camelName} !== '') payload.{camelName} = parseInt(payload.{camelName}, 10);";

            }

            if (c.LogicalType == "decimal")

            {

                return $"    if (payload.{camelName} !== undefined && payload.{camelName} !== '') payload.{camelName} = parseFloat(payload.{camelName});";

            }

            return "";

        }).Where(s => !string.IsNullOrEmpty(s)));



        var requiredFieldValidation = writable.Count > 0

            ? $$"""

                if (!payload.{{NameFactory.ToCamelCase(writable[0].Name)}}) {

                  setError('Please fill in all required fields.');

                  return;

                }

              """

            : "";



        var stateAndLoad = isNew ? "" : $$"""

              const [formData, setFormData] = useState<Record<string, any>>({});

              useEffect(() => {

                if (!id) return;

                {{api}}.get(id).then((row: any) => {

                  const normalized: Record<string, any> = {};

                  Object.entries(row).forEach(([k, v]) => {

                    normalized[k] = v;

                    normalized[k.toLowerCase()] = v;

                    normalized[k.charAt(0).toLowerCase() + k.slice(1)] = v;

                  });

                  setFormData(normalized);

                }).catch((e: Error) => setError(e.message));

              }, [id]);



              function handleChange(e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) {

                const { name, value } = e.target;

                setFormData(prev => ({

                  ...prev,

                  [name]: value,

                  [name.toLowerCase()]: value

                }));

              }

            """;



        return $$"""

            'use client';

            import { FormEvent, useEffect, useState } from 'react';

            import { useParams, useRouter } from 'next/navigation';

            import Link from 'next/link';

            import { {{api}}, getPagedItems } from '@/lib/api';

            {{fkEntityImports.ToString().TrimEnd()}}



            export default function {{entity.Name}}FormPage() {

              const router = useRouter();

              const params = useParams<{ id: string }>();

              const id = params?.id;

              const [error, setError] = useState<string>();

              const [saving, setSaving] = useState(false);

            {{fkEffects.ToString().TrimEnd()}}

            {{stateAndLoad}}

              async function onSubmit(event: FormEvent<HTMLFormElement>) {

                event.preventDefault();

                const form = new FormData(event.currentTarget);

                const raw = Object.fromEntries(form.entries());

                const payload: any = { ...raw };

            {{typeConversions}}



            {{requiredFieldValidation}}



                setSaving(true);

                setError(undefined);

                try {

                  if ({{(isNew ? "true" : "false")}}) {

                    await {{api}}.create(payload);

                  } else {

                    await {{api}}.update(id, { ...payload, {{NameFactory.ToCamelCase(pk.Name)}}: id });

                  }

                  router.push('/{{slug}}');

                } catch (e: any) {

                  setError(e instanceof Error ? e.message : 'Save failed');

                } finally {

                  setSaving(false);

                }

              }



              return (

                <section>

                  <p><Link href="/{{slug}}">← Back to {{p}}</Link></p>

                  <div className="card">

                    <h1>{{(isNew ? "Create" : "Edit")}} {{entity.Name}}</h1>

                    {error ? <div className="error">{error}</div> : null}

                    <form onSubmit={onSubmit}>

            {{fields}}

                      <div className="form-actions">

                        <button type="submit" className="btn-primary" disabled={saving}>{saving ? 'Saving…' : 'Save {{entity.Name}}'}</button>

                        <Link href="/{{slug}}" className="btn-cancel">Cancel</Link>

                      </div>

                    </form>

                  </div>

                </section>

              );

            }

            """;

    }



    private static string Dockerfile() => """

        FROM node:20.17.0-alpine AS build

        WORKDIR /app

        ARG NEXT_PUBLIC_API_URL=http://localhost:8080

        ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL

        COPY package.json ./

        RUN npm install

        COPY . .

        RUN mkdir -p /app/public

        RUN npm run build



        FROM node:20.17.0-alpine AS runtime

        WORKDIR /app

        ENV NODE_ENV=production

        COPY --from=build /app/public ./public

        COPY --from=build /app/.next/standalone ./

        COPY --from=build /app/.next/static ./.next/static

        USER node

        EXPOSE 3000

        CMD ["node", "server.js"]

        """;



    private static string AxiosInstanceFile() => """

import axios from 'axios';



const axiosInstance = axios.create({

  baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080',

  timeout: 60000,

  headers: {

    'Content-Type': 'application/json',

    'X-Custom-Header': 'client component'

  }

});



axiosInstance.interceptors.request.use(

  (config) => {

    config.headers['X-Custom-Header'] = 'custom value client';

    return config;

  },

  (error) => Promise.reject(error)

);



axiosInstance.interceptors.response.use(

  (response) => response,

  (error) => {

    return Promise.reject(error);

  }

);



export default axiosInstance;

""";



    private static string HttpStatusFile() => """

export enum HttpStatus {

  Continue = 100,

  SwitchingProtocols = 101,

  Processing = 102,

  EarlyHints = 103,

  Ok = 200,

  Created = 201,

  Accepted = 202,

  NonAuthoritativeInformation = 203,

  NoContent = 204,

  ResetContent = 205,

  PartialContent = 206,

  MultiStatus = 207,

  AlreadyReported = 208,

  ImUsed = 226,

  MultipleChoices = 300,

  MovedPermanently = 301,

  Found = 302,

  SeeOther = 303,

  NotModified = 304,

  UseProxy = 305,

  Unused = 306,

  TemporaryRedirect = 307,

  PermanentRedirect = 308,

  BadRequest = 400,

  Unauthorized = 401,

  PaymentRequired = 402,

  Forbidden = 403,

  NotFound = 404,

  MethodNotAllowed = 405,

  NotAcceptable = 406,

  ProxyAuthenticationRequired = 407,

  RequestTimeout = 408,

  Conflict = 409,

  Gone = 410,

  LengthRequired = 411,

  PreconditionFailed = 412,

  PayloadTooLarge = 413,

  UriTooLong = 414,

  UnsupportedMediaType = 415,

  RangeNotSatisfiable = 416,

  ExpectationFailed = 417,

  ImATeapot = 418,

  MisdirectedRequest = 421,

  UnprocessableEntity = 422,

  Locked = 423,

  FailedDependency = 424,

  TooEarly = 425,

  UpgradeRequired = 426,

  PreconditionRequired = 428,

  TooManyRequests = 429,

  RequestHeaderFieldsTooLarge = 431,

  UnavailableForLegalReasons = 451,

  InternalServerError = 500,

  NotImplemented = 501,

  BadGateway = 502,

  ServiceUnavailable = 503,

  GatewayTimeout = 504,

  HttpVersionNotSupported = 505,

  VariantAlsoNegotiates = 506,

  InsufficientStorage = 507,

  LoopDetected = 508,

  NotExtended = 510,

  NetworkAuthenticationRequired = 511

}

""";



    private static string ApiErrorFile() => """

import { HttpStatus } from './HttpStatus';



export interface ApiFieldError {

  Field: string;

  Description: string[];

}



export interface ApiError {

  errors?: Record<string, string[]>;

  status: HttpStatus;

  type?: string;

  title: string;

  traceId?: string;

}

""";



    private static string PagedResponseFile() => """

'use client';



export interface PagedResponse<T> {

  Data: T[];

  Page: number;

  Total: number;

  TotalPages: number;

}

""";



    private static string ErrorStateFile() => """

'use client';



import React, { createContext, useContext, useReducer, ReactNode } from 'react';



export enum GlobalStateActionType {

  Busy,

  Idle,

  Error,

  ClearError,

}



export type GlobalStateAction =

  | { type: GlobalStateActionType.Busy }

  | { type: GlobalStateActionType.Idle }

  | { type: GlobalStateActionType.Error; errorMessage: string; field?: any }

  | { type: GlobalStateActionType.ClearError };



export interface IGlobalState {

  inProgress: boolean;

  hasError: boolean;

  errorMessage?: string;

}



const initialState: IGlobalState = {

  inProgress: false,

  hasError: false,

};



const GlobalStateContext = createContext<IGlobalState>(initialState);

const GlobalDispatchContext = createContext<React.Dispatch<GlobalStateAction>>(() => {});



function globalReducer(state: IGlobalState, action: GlobalStateAction): IGlobalState {

  switch (action.type) {

    case GlobalStateActionType.Busy:

      return { ...state, inProgress: true, hasError: false, errorMessage: undefined };

    case GlobalStateActionType.Idle:

      return { ...state, inProgress: false };

    case GlobalStateActionType.Error:

      return { ...state, inProgress: false, hasError: true, errorMessage: action.errorMessage };

    case GlobalStateActionType.ClearError:

      return { ...state, hasError: false, errorMessage: undefined };

    default:

      return state;

  }

}



export function GlobalStateProvider({ children }: { children: ReactNode }) {

  const [state, dispatch] = useReducer(globalReducer, initialState);

  return (

    <GlobalStateContext.Provider value={state}>

      <GlobalDispatchContext.Provider value={dispatch}>

        {children}

      </GlobalDispatchContext.Provider>

    </GlobalStateContext.Provider>

  );

}



export function useGlobalState() {

  return useContext(GlobalStateContext);

}



export function useGlobalDispatch() {

  return useContext(GlobalDispatchContext);

}

""";



    private static string UseGetHookFile() => """

'use client';



import { useEffect, useState } from 'react';

import axiosInstance from '@/AxiosInstance';

import { GlobalStateActionType, useGlobalDispatch, useGlobalState } from '@/state/ErrorState';

import { ApiFieldError } from '@/types/ApiError';

import { useRouter } from 'next/navigation';



export function useGet<T>(baseUrl: string, url?: string, start = true) {

  const globalStateDispatch = useGlobalDispatch();

  const globalState = useGlobalState();

  const [response, setResponse] = useState<T>();

  const [inprogress, setInprogress] = useState(false);

  const router = useRouter();



  const makeCall = async (url1?: string): Promise<T | undefined> => {

    const currentUrl = url1 ?? url ?? '';

    setInprogress(true);

    try {

      globalStateDispatch({ type: GlobalStateActionType.Busy });

      const { data, status } = await axiosInstance.get<T>(baseUrl + currentUrl);

      if (status === 200 || status === 201) {

        setResponse(data);

        globalStateDispatch({ type: GlobalStateActionType.Idle });

        return data;

      } else if (status === 401) {

        router.push('/signin');

      } else {

        globalStateDispatch({

          type: GlobalStateActionType.Error,

          errorMessage: status.toString()

        });

      }

    } catch (error: any) {

      let message = error?.message || 'Error executing request';

      if (error?.response?.data) {

        const d = error.response.data;

        if (Array.isArray(d)) {

          const fieldErrors: ApiFieldError[] = d;

          message = fieldErrors.map(f => `${f.Field}: ${f.Description.join(', ')}`).join('; ');

        } else if (d.title) {

          message = d.title;

        } else if (typeof d === 'string') {

          message = d;

        }

      }

      globalStateDispatch({

        type: GlobalStateActionType.Error,

        errorMessage: message

      });

    } finally {

      setInprogress(false);

    }

  };



  useEffect(() => {

    if (start && url) {

      makeCall();

    }

  }, [url, start]);



  return { response, inprogress, makeCall, setResponse };

}

""";



    private static string UsePostHookFile() => """

'use client';



import { useState } from 'react';

import axiosInstance from '@/AxiosInstance';

import { GlobalStateActionType, useGlobalDispatch, useGlobalState } from '@/state/ErrorState';

import { ApiFieldError } from '@/types/ApiError';



export function usePost<T>(baseUrl: string, url: string, onSuccess?: (data: any) => void) {

  const globalStateDispatch = useGlobalDispatch();

  const [response, setResponse] = useState<T>();

  const [isSuccess, setIsSuccess] = useState(false);

  const globalState = useGlobalState();



  const makeCall = async (body: any) => {

    globalStateDispatch({ type: GlobalStateActionType.Busy });

    try {

      const { data, status } = await axiosInstance.post<T>(baseUrl + url, body);

      if (status === 201 || status === 200) {

        setResponse(data);

        setIsSuccess(true);

        globalStateDispatch({ type: GlobalStateActionType.Idle });

        onSuccess && onSuccess(data);

        return data;

      } else {

        globalStateDispatch({

          type: GlobalStateActionType.Error,

          errorMessage: status.toString()

        });

      }

    } catch (error: any) {

      let message = error?.message || 'Error executing post request';

      if (error?.response?.data) {

        const d = error.response.data;

        if (Array.isArray(d)) {

          const fieldErrors: ApiFieldError[] = d;

          message = fieldErrors.map(f => `${f.Field}: ${f.Description.join(', ')}`).join('; ');

        } else if (d.title) {

          message = d.title;

        } else if (typeof d === 'string') {

          message = d;

        }

      }

      globalStateDispatch({

        type: GlobalStateActionType.Error,

        errorMessage: message

      });

      return undefined;

    }

  };



  return { response, isSuccess, makeCall };

}

""";



    private static string UsePutHookFile() => """

'use client';



import { useState } from 'react';

import axiosInstance from '@/AxiosInstance';

import { GlobalStateActionType, useGlobalDispatch, useGlobalState } from '@/state/ErrorState';

import { ApiFieldError } from '@/types/ApiError';



export function usePut<T>(baseUrl: string, onSuccess?: () => void) {

  const globalStateDispatch = useGlobalDispatch();

  const [response, setResponse] = useState<T>();

  const [isSuccess, setIsSuccess] = useState(false);

  const globalState = useGlobalState();



  const makeCall = async (url: string, body?: any): Promise<T | undefined> => {

    globalStateDispatch({ type: GlobalStateActionType.Busy });

    try {

      const { data, status } = await axiosInstance.put<T>(baseUrl + url, body);

      if (status === 201 || status === 200) {

        setResponse(data);

        setIsSuccess(true);

        globalStateDispatch({ type: GlobalStateActionType.Idle });

        onSuccess && onSuccess();

        return data;

      } else {

        globalStateDispatch({

          type: GlobalStateActionType.Error,

          errorMessage: status.toString()

        });

      }

    } catch (error: any) {

      let message = error?.message || 'Error executing put request';

      if (error?.response?.data) {

        const d = error.response.data;

        if (Array.isArray(d)) {

          const fieldErrors: ApiFieldError[] = d;

          message = fieldErrors.map(f => `${f.Field}: ${f.Description.join(', ')}`).join('; ');

        } else if (d.title) {

          message = d.title;

        } else if (typeof d === 'string') {

          message = d;

        }

      }

      globalStateDispatch({

        type: GlobalStateActionType.Error,

        errorMessage: message

      });

      return undefined;

    }

  };



  return { response, isSuccess, makeCall };

}

""";



    private static string UseDeleteHookFile() => """

'use client';



import { useState } from 'react';

import axiosInstance from '@/AxiosInstance';

import { GlobalStateActionType, useGlobalDispatch } from '@/state/ErrorState';

import { ApiFieldError } from '@/types/ApiError';



export function useDelete(baseUrl: string, onSuccess?: () => void) {

  const globalStateDispatch = useGlobalDispatch();

  const [isSuccess, setIsSuccess] = useState(false);



  const makeCall = async (url: string, body?: any): Promise<any> => {

    globalStateDispatch({ type: GlobalStateActionType.Busy });

    try {

      const { data, status } = await axiosInstance.delete(baseUrl + url, {

        data: body

      });

      if (status === 201 || status === 200 || status === 204) {

        setIsSuccess(true);

        globalStateDispatch({ type: GlobalStateActionType.Idle });

        onSuccess && onSuccess();

        return data;

      } else {

        globalStateDispatch({

          type: GlobalStateActionType.Error,

          errorMessage: status.toString()

        });

        return undefined;

      }

    } catch (error: any) {

      let message = error?.message || 'Error executing delete request';

      if (error?.response?.data) {

        const d = error.response.data;

        if (Array.isArray(d)) {

          const fieldErrors: ApiFieldError[] = d;

          message = fieldErrors.map(f => `${f.Field}: ${f.Description.join(', ')}`).join('; ');

        } else if (d.title) {

          message = d.title;

        } else if (typeof d === 'string') {

          message = d;

        }

      }

      globalStateDispatch({

        type: GlobalStateActionType.Error,

        errorMessage: message

      });

      return undefined;

    }

  };



  return { isSuccess, makeCall };

}

""";



    private static string EntityApiConstantsFile(EntityDefinition entity)

    {

        var p = NameFactory.Pluralize(entity.Name);

        var slug = NameFactory.ToKebabCase(p);

        return $$"""

class {{entity.Name}}ApiConstants {

  APIBasePath = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';



  Create{{entity.Name}}Path = 'api/{{slug}}';

  {{entity.Name}}AllPath = 'api/{{slug}}';



  Get{{entity.Name}}ByIdPath = (id: any) => {

    return `${this.Create{{entity.Name}}Path}/${id}`;

  };



  GetAll{{p}}WithParams = (params: any) => {

    let url = `${this.Create{{entity.Name}}Path}?`;

    if (params) {

      Object.keys(params).forEach((key) => {

        if (params[key] !== undefined && params[key] !== null) {

          url += `&${key}=${params[key]}`;

        }

      });

    }

    return url;

  };

}



export default {{entity.Name}}ApiConstants;

""";

    }



    private static string EntityTypeFile(EntityDefinition entity)

    {

        var fields = string.Join("\n  ", entity.Columns.Select(c => $"{c.Name}?: {TsType(c)};"));

        return $$"""

export interface I{{entity.Name}} {

  {{fields}}

}

""";

    }



    private static string EntityCreateTypeFile(EntityDefinition entity)

    {

        var writable = entity.Columns.Where(c => !c.IsPrimaryKey || c.Identity == IdentityStrategy.None).ToList();

        var fields = string.Join("\n  ", writable.Select(c => $"{c.Name}: {TsType(c)};"));

        return $$"""

export interface ICreate{{entity.Name}} {

  {{fields}}

}

""";

    }



}

