# D.A.S.H. — Daily Activity and Scheduling Hub
### AI-Powered Personal Assistant | Code for Good Project

D.A.S.H. is an AI-powered personal assistant designed to simplify everyday scheduling through a conversational interface and a task management system. The project was developed by a team of 6 members as part of the Code for Good initiative.

---

## Overview

D.A.S.H. allows users to manage tasks and daily activities using natural language through a web-based chat interface. Instead of relying solely on manual forms, users can interact with the assistant conversationally to create, organize, and review tasks.

---

## Tech Stack

* **Frontend:** ASP.NET Core Razor Pages, HTML, CSS, JavaScript, Bootstrap
* **Backend:** .NET (C#), 4-layer architecture
* **Database & ORM:** SQL Server, Entity Framework Core
* **AI Integration:** Ollama running local models (TinyLlama and Mistral)
* **Testing:** xUnit, EF Core In-Memory Database Provider

---

## Architecture

The project follows a decoupled 4-layer architecture to ensure maintainability and support parallel development:

1. **Presentation Layer:** User interface, chat components, and dashboard pages.
2. **Application/Service Layer:** Core business logic, workflows, and message handling.
3. **Domain Layer:** System models, entity definitions, and core rules.
4. **Data Access Layer:** Database persistence using repositories and EF Core.

---

## Key Features

* Conversational chat interface with session history.
* Task creation and management via natural language.
* Local AI entity extraction (parsing titles, dates, and descriptions).
* Unified task dashboard for tracking and filtering activities.

---

## My Contributions

I contributed to both the technical development and the team coordination throughout the project lifecycle:

* **Requirements & Scoping:** Helped define user requirements, system use cases, and process flowcharts.
* **Technical Design:** Co-authored the technical design documentation and created the UML diagrams used to model database relationships and system flow.
* **Architecture Decisions:** Contributed implementing technical choices regarding layered architecture boundaries and backend information flow.
* **Project Coordination:** Supported team organization and milestone planning using Jira to track sprints and deliverables.
* **Quality Assurance:** Developed the backend unit test suite for the `TaskRepository` and the UI Models, validating CRUD operations in isolation using the EF Core In-Memory provider.

---

## Testing & Validation

The repository includes automated unit tests covering core backend modules, with a specific focus on repository-level CRUD operations. This setup ensured strict data integrity and maintained code stability while integrating our data layer with the local AI service.
