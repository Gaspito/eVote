# 🗳️ eVote – Web App Demo

A lightweight voting platform built as a mock technical interview project. Designed to showcase clean architecture and thoughtful user interaction.

---

## Tech Stack

- [.NET](https://dotnet.microsoft.com/) + ASP.NET MVC  
- [Entity Framework](https://learn.microsoft.com/en-us/ef/)  
- [Docker](https://www.docker.com/)  
- [RabbitMQ](https://www.rabbitmq.com/)  
- [SQLite](https://www.sqlite.org/) (for demo purposes only—swap with a robust DB in production)

---

## Purpose

This app is part of a technical interview challenge. The goal: build a functional voting platform (“eVote”) where users can register, become candidates, vote for others, and change their votes—all while respecting some quirky but clever rules.

---

## Features

- Register with an email address  
- View the voting list of users  
- Become a candidate or opt out anytime (as long as vote count is 0)
- Cast votes (with restrictions, i.e. no voting for yourself or the same candidate multiple times)  
- Change or remove votes dynamically  
- Live vote counts in descending order  
- Display of total remaining votes across all voters  
- Prevent candidates from voting, enforce vote limits and uniqueness

---

## Getting Started

> To run the demo locally:

```bash
docker pull rabbitmq
docker-compose up -d
```
Then visit http://localhost:5079/ in your browser
