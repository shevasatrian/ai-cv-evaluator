# ai-cv-evaluator

## Overview
AI CV Evaluator is a .NET 8-based backend service that automatically evaluates candidates' CVs and project reports using LLM (Large Language Model) and RAG (Retrieval-Augmented Generation) approaches.
The goal is to automate the initial candidate selection process by assessing the relevance of CVs and the quality of project reports against job descriptions and predetermined evaluation criteria.

## Objectives
- Upload and save candidates' CVs and Project Reports.
- Performing AI evaluation on both documents using internal context:
  - Job Description
  - Case Study Brief
  - CV Rubric
  - Project Rubric
- Produce structured evaluation reports containing:
  - cv_match_rate
  - cv_feedback
  - project_score
  - project_feedback
  - overall_summary
- Providing asynchronous API endpoints so that pipelines do not hinder main requests.

## Features & Endpoints
1. Upload CV & Project
```
POST /upload
```
- Accepts multipart/form-data
- Fields:
  - cvFile
  - projectFile
- Returns file paths for evaluation
2. Trigger Evaluation
```
POST /evaluate
```
Request
```
{
  "jobTitle": "Back End Engineer",
  "cvFilePath": "Uploads/example_CV.pdf",
  "reportFilePath": "Uploads/example_Project.pdf"
}
```
Response
```
{
  "id": "caff41e6-1f20-4c96-a23a-ac9b9812eb8c",
  "status": "queued"
}
```
3. Get Evaluation Result
```
GET /evaluate/{id}
```
4. Ingest Ground Truth Documents (RAG)
```
POST /admin/ingest?source=job_description
```
Body: raw text or PDF file.
Supported sources:
- job_description
- case_study
- cv_rubric
- project_rubric
Each file is converted into an embedding and stored in the DocumentVectors table

## How to Run Locally
1. Clone repository
2. Add appsettings.json
   ApiKey: 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
```
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ai_cv;Username=postgres;Password=yourpassword"
  },
  "OpenAI": {
    "ApiKey": "sk-xxxxxx"
  }
}
```
3. Run migration (Use PostgreSQL)
```
dotnet ef migrations add InitialCreate --project ./ai-cv-evaluator
dotnet ef database update --project ./ai-cv-evaluator
```
4. Run Application

## Design Choices
| Aspect                 | Decision                               | Reason                                                                         |
| ---------------------- | -------------------------------------- | ------------------------------------------------------------------------------ |
| **Framework**          | .NET 8 Web API                         | Supports background workers, dependency injection, and asynchronous pipelines. |
| **LLM**                | GPT-4o-mini                            | Fast, token-efficient, suitable for textual evaluation.                        |
| **Embeddings (RAG)**   | text-embedding-3-small                 | Lightweight and economical for similarity searches.                            |
| **Vector DB (RAG)**    | EF Core + JSON                         | Avoiding external dependencies such as Qdrant, more portable.                  |
| **Async Job Handling** | BackgroundService (`EvaluationWorker`) | Avoiding blocking requests during AI evaluation.                               |
| **Prompt Design**      | Structured JSON output per tahap       | Simplifies parsing and storage of results.                                     |
| **Error Handling**     | Try-Catch + status update di DB        | So that failed jobs are still recorded and the system does not crash.          |

