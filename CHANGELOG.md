# MelloSilveira tools
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## UNRELEASED
### Added
- Logger.
- GetDeleteByPrimaryKeyQuery, GetSelectByPrimaryKeyQuery and GetUpdateByPrimaryKeyQuery methods on ISqlProvider.
- Extension methods evolving NPGSQL classes, IFormFile, Type and string.
- JWE authentication.
- Methods to enable Swagger using v1.
- Service for encryption.
- String builder using pooled buffers and stack allocation optimizations.
- Resilience pipeline for default cases and PostgreSQL repositories.
- Basic and PostgreSQL implementations for database repository.
- Basic implementations for use cases operations.
- Basic implementations for API service agents.
### Removed
- ReferencedPropertyName property from ForeignKeyColumnAttribute.

## [1.0.3] - 2025-06-11
### Added
- TableAttribute constructor receiving name and alias.

## [1.0.2] - 2025-06-10
### Fixed
- Project informations.

## [1.0.1] - 2025-06-10
### Removed
- Unnecessary build files.

## [1.0.0] - 2025-06-10
### Added
- First version of the program.