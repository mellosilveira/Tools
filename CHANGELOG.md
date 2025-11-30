# MelloSilveira tools
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.1.1] - 2025-11-XX
### Added
- DifferentialEquationMethodFactory.
- Method for Mechanics of Materials dependencies.
- CurveType enum.
- CreateSuccessOk method for OperationResponseBase with response data.
### Renamed
- Fatigue to FatigueCalculator.
- GeometricProperty to GeometricPropertyCalculator.
- MechanicsOfMaterials to ConstitutiveEquationsCalculator.

## [1.1.0] - 2025-11-29
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
- Mechanical of materials.
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