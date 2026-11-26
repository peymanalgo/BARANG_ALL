# Production Planning and Execution

Phase 15 models production as company-owned `ProductionJob` records with embedded deterministic plan fields and an append-only `ProductionJobHistory`. Job types are `DesignPreparation`, `Print`, and `HeatPress`; the lifecycle is `Planned -> Ready -> InProgress -> Completed`, with `Failed` and `InProgress -> Suspended -> InProgress` branches. All transitions are domain methods—clients cannot assign status directly.

Jobs are generated idempotently from a submitted Order. Each active item produces design-preparation and heat-press jobs, while each active print operation produces one print job linked to its exact frozen operation, ArtworkVersion, and FileVersion. The company/job generation key is unique, so retries return existing jobs without duplicate history.

Planning sets priority, UTC window, and deterministic queue position. Operators must be active, have current company access, and hold `production.execute`. Print equipment must have an active usable status and a printing type; heat-press equipment must have a heat/press type. Design preparation needs an operator but no equipment. Resume revalidates assignments.

Queue queries support status, job type, operator, equipment, equipment branch, priority, planned date, bounded pagination, and stable priority/queue/time ordering. Capacity is intentionally lightweight: active equipment, planned and current job counts, current job, and the requested planned window. It is not optimization or OEE.

Permissions are `production.read`, `production.plan`, `production.assign`, and `production.execute`. EF company filters and PostgreSQL RLS protect jobs and history; same-company foreign keys protect Order, OrderItem, PrintOperation, Equipment, ArtworkVersion, and FileVersion references. Mutable jobs use optimistic concurrency; stale commands return 409. Required audit and concise business history are written in the same PostgreSQL transaction. There are no DELETE APIs or grants.

QC/Reproduction, packaging/shipping, maintenance, workflow orchestration, payroll, and OEE remain future phases.
