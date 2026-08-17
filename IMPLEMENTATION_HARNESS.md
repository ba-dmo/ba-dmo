\# BA DMO — IMPLEMENTATION HARNESS



\## 0. NON-AUTHORITY / CONFLICT RULE



This harness is an EXECUTION PROTOCOL only.



It is NOT:

\- a business specification;

\- a domain specification;

\- an architecture specification;

\- a design specification;

\- a data contract;

\- a replacement for Plan-V3.



It MUST NOT introduce, override, reinterpret, extend or weaken any Plan-V3 requirement.



If this harness conflicts with Plan-V3 in any way:



PLAN-V3 WINS.



Do not attempt to reconcile the conflict by inventing a compromise.



Ignore the conflicting harness instruction and follow Plan-V3.



If the conflict is material enough that execution cannot continue safely:

report HARNESS CONFLICT and stop.



Authority precedence:



1\. Current explicit owner instruction

2\. Plan-V3 canonical package

3\. Design-Reference — UI/UX/presentation authority only

4\. Verified legacy evidence when explicitly required

5\. Safe implementation detail

6\. This harness, ONLY for execution/process behavior



The harness is intentionally LAST in authority.



A harness instruction can control:

\- work cadence;

\- autonomy;

\- testing discipline;

\- reporting format;

\- state tracking;

\- scope control;

\- when to stop.



A harness instruction CANNOT define:

\- business behavior;

\- domain rules;

\- permissions;

\- data model;

\- workflows;

\- architecture;

\- module ownership;

\- UI/UX requirements;

\- deployment contracts;

\- validation rules;

\- formulas;

\- security policy.



If Plan-V3 already specifies something, follow Plan-V3 literally.



Lack of detail in this harness is NEVER a blocker.



The harness is not expected to contain implementation requirements.



Search the authoritative Plan-V3 documents first.



\---



\## 1. AUTHORITY



Implementation authority:



C:\\BA-DMO-REFERENCE\\ba-dmo-beta\\Spec\\Plan-V3\\output\\QWEN\_GLM\_5\_3\_IMPLEMENTATION\_HANDOFF



Design authority:



C:\\BA-DMO-REFERENCE\\ba-dmo-beta\\Design-Reference\\portal-dmo-design-final



Current primary implementation agent:



Qwen 3.8 Max



Plan-V3 is authoritative.



Plan-V1 and Plan-V2 are historical only.



Do not reinterpret confirmed business rules.



Legacy is optional evidence only.



If a MATERIAL requirement is genuinely missing or contradictory after checking the relevant Plan-V3 documents:



STOP and report BLOCKED.



Do not invent.



\---



\## 2. WORKSPACE



READ-ONLY REFERENCE:



C:\\BA-DMO-REFERENCE\\ba-dmo-beta



WRITE WORKSPACE:



C:\\BA-DMO-FRESH-BUILD



Do not modify:



C:\\BA-DMO-REFERENCE\\ba-dmo-beta



Do not modify:

\- Plan-V3;

\- Plan-V2;

\- Plan-V1;

\- Design-Reference;

\- archived manifests;

\- archived provenance.



Do not implement application code inside ba-dmo-beta.



All fresh-build application code belongs only in:



C:\\BA-DMO-FRESH-BUILD



\---



\## 3. FRESH BUILD RULE



THIS IS A TRUE FRESH BUILD.



Do not:

\- copy the previous C# solution as implementation base;

\- copy old src folders wholesale;

\- copy legacy Razor Pages wholesale;

\- copy old JavaScript runtime architecture;

\- inherit debug authentication hacks;

\- inherit obsolete migrations blindly;

\- inherit legacy localStorage/domain datastore patterns;

\- patch the old application instead of building the approved architecture.



Legacy may be read only when explicitly required as evidence.



Legacy is never the implementation base.



\---



\## 4. EXECUTION MODEL



Implement exactly ONE authorized roadmap unit at a time.



For the current unit:



READ

→ INSPECT CURRENT STATE

→ IMPLEMENT

→ BUILD

→ TEST

→ FIX

→ RETEST

→ REGRESSION CHECK

→ UPDATE IMPLEMENTATION STATE

→ REPORT

→ STOP



Do not automatically begin the next unit.



Do not widen scope without evidence.



Do not mix future units into the current unit merely because they seem convenient.



\---



\## 5. AUTONOMY — NO BABYSITTING



You are expected to work autonomously inside the approved unit.



Do NOT stop to ask routine questions such as:



\- may I create this class?

\- may I create this interface?

\- may I create this file?

\- should I continue?

\- should I fix this compiler error?

\- should I add the test required by the spec?

\- should I refactor this private method?

\- should I correct this typo in code I just created?

\- should I retry the failed build?



If the action is:



\- inside the current unit scope;

\- consistent with Plan-V3;

\- not a material business decision;

\- not destructive;

\- not a prohibited Git/database action;

\- required to complete the unit or make build/tests pass;



perform it autonomously.



Do not ask the owner to babysit normal development work.



\---



\## 6. FIX YOUR OWN ERRORS



If build or tests fail because of your implementation:



investigate and fix them yourself.



Do not return ordinary implementation failures to the owner as blockers.



Examples of things you must normally fix yourself:



\- compiler errors;

\- namespace errors;

\- missing project references;

\- incorrect DI registrations;

\- test failures caused by your implementation;

\- nullability warnings when relevant;

\- broken internal paths;

\- incorrect mappings;

\- malformed local configuration templates;

\- CSS/markup mistakes within current unit;

\- incorrect test setup created by you.



Repeat:



IMPLEMENT

→ BUILD

→ TEST

→ FIX



until the required gate is green or a genuine material blocker is proven.



\---



\## 7. BLOCKER DEFINITION



STOP only for a genuine MATERIAL blocker.



Examples:



\- two authoritative Plan-V3 requirements materially contradict each other;

\- required business behavior is genuinely undefined;

\- implementation requires a business decision not present in Plan-V3;

\- a required external credential/access is unavailable;

\- required live SQL/database operation needs explicit owner approval;

\- required destructive operation needs owner approval;

\- implementation would require changing an approved architecture/security/data contract;

\- a required external service is unavailable and no approved fallback exists.



Do NOT classify as blockers:



\- compiler errors;

\- test failures caused by your code;

\- naming choices;

\- internal class structure;

\- routine refactoring;

\- safe implementation choices;

\- details explicitly left open by Plan-V3;

\- lack of information in this harness.



When blocked report exactly:



BLOCKER TYPE:

BUSINESS / TECHNICAL CONTRACT / SECURITY / EXTERNAL ACCESS / HARNESS CONFLICT



FILE:

<authoritative file>



SECTION:

<section/id>



EVIDENCE:

<exact conflicting/missing requirement>



WHY EXECUTION CANNOT CONTINUE:

<reason>



MINIMUM OWNER DECISION REQUIRED:

<smallest decision needed>



Do not invent a workaround that changes the contract.



\---



\## 8. SAFE IMPLEMENTER CHOICES



You may independently choose non-material implementation details when Plan-V3 allows them.



A choice is only SAFE if:



\- Plan-V3 explicitly leaves it open;



OR



\- changing that choice cannot affect externally observable behavior, business semantics, persisted data, authorization, security, integrations, deployment contract or required UX.



Examples of safe choices may include:



\- private/internal class names;

\- private method names;

\- equivalent interface names where the contract allows equivalents;

\- folder organization inside approved project/module boundaries;

\- local development port;

\- internal helper implementations;

\- test helper naming;

\- standard language/framework mechanics.



Before treating something as a safe choice:



consult the relevant Plan-V3 section.



Do not use this harness as evidence that something is safe.



If uncertain, search Plan-V3 first.



\---



\## 9. SCOPE CONTROL



Do not:



\- implement future roadmap units;

\- redesign architecture;

\- add speculative functionality;

\- introduce new modules;

\- introduce microservices;

\- introduce SPA architecture;

\- replace Razor Pages with another frontend architecture;

\- replace approved persistence architecture;

\- introduce EF Core as a replacement for approved Npgsql/Dapper architecture;

\- move business rules into JavaScript;

\- create parallel domain models;

\- create parallel catalogs;

\- introduce local domain databases;

\- introduce localStorage as domain storage;

\- introduce IndexedDB as domain storage;

\- silently improve or reinterpret confirmed business behavior;

\- refactor unrelated completed units merely for style.



Touch only what is reasonably required by the current authorized unit.



\---



\## 10. PLAN-V3 READING STRATEGY



Do not reread the entire repository unnecessarily.



For every unit:



1\. Read:



&#x20;  00\_START\_HERE.md



2\. Read the exact unit in:



&#x20;  10\_MASTER\_IMPLEMENTATION\_ROADMAP.md



3\. Read every authoritative document referenced by that unit.



4\. Read the relevant module spec.



5\. Read directly dependent architecture/data/access/design/test contracts.



6\. Inspect existing fresh-build code affected by the unit.



7\. Expand context only when evidence requires it.



Do not load Plan-V1/Plan-V2 unless provenance is explicitly required.



Do not search legacy automatically.



\---



\## 11. DESIGN AUTHORITY



Design-Reference is authoritative for:



\- UI/UX;

\- presentation;

\- visual hierarchy;

\- component behavior;

\- layout;

\- interaction patterns;

\- design tokens;

\- visual states.



Design-Reference does NOT override:



\- confirmed business rules;

\- domain invariants;

\- security;

\- access control;

\- data ownership;

\- technical contracts.



Do not invent business logic based on a mockup.



Do not copy mockup HTML/CSS blindly if Plan-V3 defines a different implementation architecture.



Implement the design through the approved design-system architecture.



\---



\## 12. TEST DISCIPLINE



Tests are part of implementation.



They are not optional cleanup after coding.



For every unit:



1\. implement required production code;

2\. implement the tests required by Plan-V3;

3\. run targeted unit tests;

4\. run targeted integration tests when applicable and authorized;

5\. fix failures;

6\. rerun;

7\. run broader relevant regression tests where practical.



Never declare COMPLETE if:



\- required build fails;

\- required tests fail;

\- required negative authorization tests fail;

\- a known regression introduced by the unit remains unfixed.



Do not delete or weaken a valid test merely to make the suite green.



If a test contradicts Plan-V3:



investigate the authority conflict.



\---



\## 13. BUILD DISCIPLINE



Use the approved technical target:



.NET 10

ASP.NET Core 10

Razor Pages



Expected solution architecture:



BA-DMO.sln



Projects:



src\\BA.Dmo.Domain

src\\BA.Dmo.Application

src\\BA.Dmo.Infrastructure

src\\BA.Dmo.Web



tests\\BA.Dmo.UnitTests

tests\\BA.Dmo.IntegrationTests



Canonical dependency direction:



Application → Domain



Infrastructure → Application + Domain



Web → Application + Infrastructure



UnitTests → Domain + Application



IntegrationTests → Web + Infrastructure



Do not introduce dependency cycles.



\---



\## 14. DATABASE / SQL SAFETY



Do not execute SQL against live Supabase without explicit owner authorization.



Creating migration files inside the approved unit is allowed when the unit requires them.



Running live migrations is a separate action requiring authorization where Plan-V3 says so.



Migration architecture must remain:



\- forward-oriented scripts;

\- Npgsql execution;

\- whole-script execution;

\- SHA-256 tracking;

\- schema\_migrations;

\- no custom semicolon parser;

\- no split(';');

\- no HTTP migration endpoint;

\- no automatic production startup migration.



Never place secrets into repository files.



\---



\## 15. AUTH / SECURITY



Preserve Plan-V3 security boundaries.



Do not:



\- introduce anonymous admin;

\- introduce debug authentication bypass;

\- introduce insecure fallback identity;

\- store privileged service-role keys in browser code;

\- trust client-side visibility as authorization;

\- store grants only in cookies and trust them without server-side resolution;

\- expose database tables directly to browser modules.



Authorization must be enforced server-side according to Plan-V3.



\---



\## 16. DOMAIN PRINCIPLE



The BA DMO records operational facts, traceability and history.



It does not become a prediction or recommendation engine.



Do not:



\- invent operational facts;

\- silently correct user facts;

\- infer unknown operational state;

\- block real facts using unsupported heuristics;

\- convert warnings into hard blocks without confirmed authority.



Any hard block must be justified by:



SECURITY



TECHNICAL INTEGRITY



or



CONFIRMED BUSINESS RULE



Warnings remain warnings.



\---



\## 17. AUDIT PRINCIPLE



Business-relevant actions must preserve traceability according to Plan-V3.



Do not silently rewrite historical facts.



Do not delete audit history.



Corrections must remain auditable when required.



No scoring.

No rankings.

No performance judgement logic unless explicitly specified in future owner decisions.



\---



\## 18. CLIENT STORAGE



Domain data must NOT use:



localStorage



or



IndexedDB



as source of truth.



Approved IndexedDB exception:



ONLY technical persistence of FileSystemDirectoryHandle / permission state where Plan-V3 explicitly allows it.



Currently approved examples include:



\- PDF local directory handle where applicable;

\- Job On image directory handle.



Do not expand this exception to domain data.



\---



\## 19. JOB ON IMAGE DIRECTORY



Follow Plan-V3 owner clarification.



Job On image access uses:



File System Access API



The UI exposes the approved directory-link action.



IndexedDB may persist only:



FileSystemDirectoryHandle / technical permission state.



Do NOT:



\- require Supabase Storage for Job On images;

\- store image binary in PostgreSQL;

\- use Render filesystem as image storage;

\- depend on absolute Windows paths as browser access mechanism.



If permission is lost:



request reauthorization.



Do not invalidate the Job On business record.



Image association remains per revision and auditable.



\---



\## 20. PDF EXPORT



Preserve the approved PDF boundary:



C# backend

→ PDF bytes in memory

→ HTTP binary/FileResult

→ browser Blob

→ File System Access API when available

→ standard download fallback



Do not store user PDFs on Render filesystem as application storage.



PDF renderer implementation may remain a safe implementation choice if Plan-V3 leaves the concrete library open.



\---



\## 21. DEPLOYMENT CONTRACT



Production target:



GitHub

→ Render

→ Docker build

→ Linux ASP.NET Core container

→ Supabase

→ browser users



User workstations are thin clients.



Do not require users to install:



\- .NET;

\- Docker;

\- Node;

\- PostgreSQL;

\- Supabase CLI;

\- application executables.



Production is not:



\- win-x64 executable deployment;

\- Windows Service;

\- IIS requirement;

\- local backend installation.



Respect dynamic Render port configuration.



\---



\## 22. GIT RULES



Git is the implementation logbook.



Before starting every unit record:



\- current branch;

\- current HEAD;

\- working tree status.



Never:



\- force push;

\- rewrite history;

\- reset unrelated work;

\- overwrite unrelated changes;

\- delete history to simplify implementation.



Do not commit automatically unless the owner has explicitly authorized commits for the implementation workflow.



If commits are authorized:



\- keep commits scoped;

\- use descriptive messages;

\- do not combine unrelated future work;

\- record resulting SHA in IMPLEMENTATION\_STATE.md.



A clean build/test does not automatically authorize a commit.



\---



\## 23. IMPLEMENTATION STATE



Maintain:



C:\\BA-DMO-FRESH-BUILD\\IMPLEMENTATION\_STATE.md



This is operational recovery state.



It does NOT override Plan-V3.



Update it after every completed or blocked unit.



Minimum contents:



\# BA DMO Implementation State



Workspace:

C:\\BA-DMO-FRESH-BUILD



Reference repository:

C:\\BA-DMO-REFERENCE\\ba-dmo-beta



Implementation authority:

Plan-V3



Current branch:

<actual>



Current HEAD:

<actual>



Current unit:

<U-ID>



Status:

IN PROGRESS / COMPLETE / BLOCKED



Completed units:

<list>



\## Last Unit Summary



\## Files Created/Changed



\## Build



\## Tests Executed



\## Test Results



\## Decisions Applied



\## Safe Implementer Choices Made



\## Blockers



\## Known Risks



\## Manual Checks Pending



\## Next Unit



\## Git Commit



\## Notes for Next Agent Session



Do not put secrets inside IMPLEMENTATION\_STATE.md.



\---



\## 24. RECOVERY AFTER SESSION / CACHE LOSS



If starting in a new session:



1\. read this harness;

2\. read IMPLEMENTATION\_STATE.md;

3\. verify Git HEAD/status;

4\. read 00\_START\_HERE;

5\. read the current roadmap unit;

6\. read that unit's referenced specs;

7\. inspect changed/current implementation files;

8\. continue from evidence.



Do not reread all historical plans by default.



Do not assume previous conversational memory is available.



Repository + Plan-V3 + implementation state are the recovery mechanism.



\---



\## 25. NO SILENT CONTRACT CHANGES



If implementation reveals that Plan-V3 appears wrong or incomplete:



do not silently repair the specification through code.



Do not change business behavior to make implementation easier.



Report the evidence.



If material:



STOP.



If merely a safe implementation detail:



choose it and record it.



\---



\## 26. HARNESS COMPATIBILITY SELF-CHECK



At the beginning of the first implementation session only:



compare this harness against Plan-V3 for process-level conflicts.



Do NOT perform another full functional audit.



Verify only that:



\- harness does not override Plan-V3;

\- workspace paths are correct;

\- reference repository exists;

\- Plan-V3 exists;

\- Design-Reference exists;

\- IMPLEMENTATION\_STATE.md exists or can be created;

\- current unit can be identified.



Expected result:



HARNESS ↔ PLAN-V3:

PASS



If a harness conflict exists:



Plan-V3 wins.



Correct execution according to Plan-V3 and report the harness conflict.



Do not reinterpret Plan-V3 to match the harness.



\---



\## 27. END-OF-UNIT REPORT



At the end of every unit return:



\## UNIT

<U-ID>



\## STATUS

COMPLETE / BLOCKED



\## BASELINE

Branch:

HEAD:

Working tree before:



\## FILES CREATED/CHANGED

<list>



\## BUILD

PASS / FAIL



Command(s):

<commands>



\## TESTS

Total:

Passed:

Failed:

Duration:



\## REGRESSION CHECK

PASS / FAIL / NOT APPLICABLE



\## MANUAL CHECKS PENDING

<list or NONE>



\## SAFE IMPLEMENTER CHOICES MADE

<list or NONE>



\## DISCREPANCIES

<list or NONE>



\## BLOCKERS

<list or NONE>



\## IMPLEMENTATION\_STATE UPDATED

YES / NO



\## CURRENT GIT STATUS

<actual>



\## NEXT UNIT

<U-ID>



\## NEXT UNIT STARTED

NO



Then STOP.



\---



\## 28. DEFAULT BEHAVIOR



Default behavior inside an authorized unit:



BE AUTONOMOUS.



Do not wait for permission for routine work.



Read the authority.

Implement the unit.

Build it.

Test it.

Fix it.

Retest it.

Record state.

Report evidence.

Stop at the unit/gate boundary.



Only involve the owner when a genuine material decision is required.

