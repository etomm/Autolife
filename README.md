# Autolife

**AI-Enabled Personal Life and Knowledge Management Platform**

## Overview

Autolife is a comprehensive web platform designed to manage all aspects of your life and knowledge through AI-powered assistance. Think of it as your personal manual where all experiences, documents, and projects are explored, managed, and interconnected.

## Core Architecture

### Three Content Pillars

1. **Knowledge** - Your personal experience repository
   - Detailed guides and how-tos (e.g., "How to build a house")
   - Personal learnings and insights
   - Searchable information database
   - AI-powered knowledge retrieval

2. **Documents** - Automatic document management system
   - Upload and storage of all personal documents
   - Automatic organization and categorization
   - Document linking to projects and knowledge entries
   - Version-controlled storage

3. **Projects** - Ongoing project tracking
   - Create projects from knowledge guides
   - Automatic folder creation in Documents section
   - Progress tracking and milestones
   - AI-assisted project planning

### Technical Stack

- **Backend**: ASP.NET Core 8.0 (C#)
- **Frontend**: Blazor Server with Bootstrap 5
- **AI Framework**: Customizable AI models and agents
  - Plugin architecture for multiple AI providers (OpenAI, Azure OpenAI, local models)
  - Agent-based task automation
- **Storage**: Git-based content versioning
  - All content stored in git repositories
  - Full rollback capabilities
  - Distributed backup support
- **Device Management**: Network and device integration (where supported)

## Project Structure

```
Autolife/
├── src/
│   ├── Autolife.Web/              # Main web application (Blazor Server)
│   ├── Autolife.Core/             # Core business logic and domain models
│   ├── Autolife.AI/               # AI agent framework and integrations
│   ├── Autolife.Storage/          # Git-based storage abstraction
│   └── Autolife.DeviceManager/    # Network and device management
├── tests/
│   └── Autolife.Tests/            # Unit and integration tests
├── docs/
│   └── architecture.md            # Detailed architecture documentation
└── .gitignore
```

## Key Features

### AI Capabilities
- **Customizable AI Models**: Plug in your preferred AI provider
- **Intelligent Agents**: Automated task execution and content organization
- **Natural Language Queries**: Search knowledge using conversational language
- **Smart Suggestions**: AI-powered recommendations for related content

### Content Management
- **Bidirectional Linking**: Connect knowledge, documents, and projects
- **Version Control**: Every change is tracked via Git
- **Full-Text Search**: Find anything across all three pillars
- **Tagging and Categorization**: Automatic and manual organization

### Device Integration
- Network device discovery and management
- Remote access capabilities (where supported)
- Device state monitoring

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Git
- (Optional) Docker for containerized deployment

### Running Locally

```bash
# Clone the repository
git clone https://github.com/etomm/Autolife.git
cd Autolife

# Restore dependencies
dotnet restore

# Run the web application
cd src/Autolife.Web
dotnet run
```

Navigate to `https://localhost:5001` in your browser.

### Configuration

Copy `appsettings.example.json` to `appsettings.json` and configure:
- AI provider settings (API keys, endpoints)
- Git storage location
- Device management settings

## Development Roadmap

- [x] Initial project structure
- [ ] Core content models (Knowledge, Documents, Projects)
- [ ] Git-based storage implementation
- [ ] Basic UI with navigation
- [ ] AI agent framework
- [ ] AI provider integrations (OpenAI, Azure OpenAI)
- [ ] Document upload and management
- [ ] Knowledge search and retrieval
- [ ] Project creation and tracking
- [ ] Device discovery and management
- [ ] Advanced AI features (summarization, extraction, automation)

## Contributing

This is a personal project, but suggestions and ideas are welcome via Issues.

## License

MIT License - See LICENSE file for details

## Architecture Philosophy

**Git as the Source of Truth**: All user content (knowledge, documents, projects) is stored in Git repositories, providing:
- Complete version history
- Easy backup and restore
- Distributed storage options
- Rollback to any previous state

**AI as the Assistant**: AI doesn't replace human decision-making but enhances it:
- Automates repetitive tasks
- Provides intelligent suggestions
- Extracts insights from content
- Connects related information

**Modular and Extensible**: Plugin architecture allows:
- Multiple AI providers
- Custom storage backends
- Additional device integrations
- Community extensions
