# ADR 0001 · ASP.NET Core + Angular + SQLite

**Estado:** aceptada

## Contexto

El reto permite cualquier lenguaje o framework. Pide una API sencilla, una base de datos local (SQLite o similar), guardar archivos en una carpeta del servidor y que se pueda ejecutar en local sin desplegar en la nube.

## Decisión

- **Backend:** ASP.NET Core (.NET 10, versión LTS) con controladores y Entity Framework Core.
- **Frontend:** Angular 22 con componentes standalone, signals y formularios reactivos.
- **Base de datos:** SQLite mediante EF Core, con migraciones versionadas.

## Consecuencias

- Tipado estático de punta a punta (C# y TypeScript): menos errores en tiempo de ejecución.
- SQLite no requiere instalar nada; cambiar a PostgreSQL o SQL Server solo exige cambiar el proveedor en `Practicas.Infrastructure`.
- SQLite no guarda zonas horarias: todas las fechas se almacenan en UTC y un conversor las marca como UTC al leerlas.
- La aplicación se puede servir en un solo proceso (la API sirve el frontend compilado), en Docker o en modo desarrollo con dos procesos.
