-- Run only in the independent test/Beta project. All test rows are rolled back.
BEGIN;
DO $verify$
DECLARE
  tenant_id uuid := gen_random_uuid();
  user_a uuid := gen_random_uuid();
  user_b uuid := gen_random_uuid();
  workout_a uuid := gen_random_uuid();
  workout_b uuid := gen_random_uuid();
  exercise_id uuid := gen_random_uuid();
  active_id uuid := gen_random_uuid();
  operation_id uuid := gen_random_uuid();
  statement text;
  visible_id uuid;
  row_count integer;
BEGIN
  IF (SELECT count(*) FROM public."__EFMigrationsHistory") <> 7 THEN
    RAISE EXCEPTION 'Expected seven EF migrations';
  END IF;
  IF EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'app' AND c.relkind = 'r' AND NOT c.relrowsecurity) THEN
    RAISE EXCEPTION 'An application table has RLS disabled';
  END IF;

  INSERT INTO app."Tenants" ("Id", "Nome", "Tipo", "Ativo", "CriadoEm", "RowVersion")
    VALUES (tenant_id, 'Transactional verification', 'Individual', true, now(), decode('01', 'hex'));
  INSERT INTO app."Usuarios" ("Id", "TenantId", "IdentityUserId", "Nome", "Email", "Status", "Role", "CriadoEm", "RowVersion")
    VALUES (user_a, tenant_id, user_a, 'A', user_a::text || '@example.test', 1, 1, now(), decode('01', 'hex')),
           (user_b, tenant_id, user_b, 'B', user_b::text || '@example.test', 1, 1, now(), decode('01', 'hex'));
  INSERT INTO app."TreinosUsuario" ("Id", "TenantId", "UsuarioId", "Nome", "Objetivo", "FrequenciaSemanal", "Versao", "DataInicio", "DuracaoSemanas", "Fase", "Ativo", "CriadoEm", "RowVersion")
    VALUES (workout_a, tenant_id, user_a, 'A workout', 'Forca', 3, 1, current_date, 6, 'Base', true, now(), decode('01', 'hex')),
           (workout_b, tenant_id, user_b, 'B workout', 'Forca', 3, 1, current_date, 6, 'Base', true, now(), decode('01', 'hex'));
  INSERT INTO app."Exercicios" ("Id", "TenantId", "Nome", "GrupoMuscular", "Nivel", "Instrucao", "CriadoEm", "RowVersion")
    VALUES (exercise_id, tenant_id, 'Verification', 'Peitoral', 'Base', 'Verification', now(), decode('01', 'hex'));
  INSERT INTO app."TreinosExercicios" ("Id", "TenantId", "TreinoUsuarioId", "ExercicioId", "Ordem", "DiaTreino", "Series", "Repeticoes", "DescansoSegundos", "CriadoEm", "RowVersion")
    VALUES (active_id, tenant_id, workout_a, exercise_id, 1, 1, 3, '8-12', 60, now(), decode('01', 'hex'));
  BEGIN
    INSERT INTO app."TreinosExercicios" ("Id", "TenantId", "TreinoUsuarioId", "ExercicioId", "Ordem", "DiaTreino", "Series", "Repeticoes", "DescansoSegundos", "CriadoEm", "RowVersion")
      VALUES (gen_random_uuid(), tenant_id, workout_a, exercise_id, 1, 1, 3, '8-12', 60, now(), decode('01', 'hex'));
    RAISE EXCEPTION 'Active exercise duplication was accepted';
  EXCEPTION WHEN unique_violation THEN NULL;
  END;
  UPDATE app."TreinosExercicios" SET "ExcluidoEm" = now() WHERE "Id" = active_id;
  INSERT INTO app."TreinosExercicios" ("Id", "TenantId", "TreinoUsuarioId", "ExercicioId", "Ordem", "DiaTreino", "Series", "Repeticoes", "DescansoSegundos", "CriadoEm", "RowVersion")
    VALUES (gen_random_uuid(), tenant_id, workout_a, exercise_id, 1, 1, 3, '8-12', 60, now(), decode('01', 'hex'));

  INSERT INTO app."TreinosSessoes" ("Id", "TenantId", "UsuarioId", "TreinoUsuarioId", "OperationId", "DiaTreino", "Data", "IniciadoEm", "CriadoEm", "RowVersion")
    VALUES (gen_random_uuid(), tenant_id, user_a, workout_a, operation_id, 1, current_date, now(), now(), decode('01', 'hex'));
  BEGIN
    INSERT INTO app."TreinosSessoes" ("Id", "TenantId", "UsuarioId", "TreinoUsuarioId", "OperationId", "DiaTreino", "Data", "IniciadoEm", "CriadoEm", "RowVersion")
      VALUES (gen_random_uuid(), tenant_id, user_a, workout_a, operation_id, 1, current_date, now(), now(), decode('01', 'hex'));
    RAISE EXCEPTION 'Duplicate operation ID was accepted';
  EXCEPTION WHEN unique_violation THEN NULL;
  END;
  BEGIN
    INSERT INTO app."TreinosSessoes" ("Id", "TenantId", "UsuarioId", "TreinoUsuarioId", "DiaTreino", "Data", "IniciadoEm", "CriadoEm", "RowVersion")
      VALUES (gen_random_uuid(), tenant_id, user_a, gen_random_uuid(), 1, current_date, now(), now(), decode('01', 'hex'));
    RAISE EXCEPTION 'Invalid workout foreign key was accepted';
  EXCEPTION WHEN foreign_key_violation THEN NULL;
  END;
  BEGIN
    UPDATE app."TreinosUsuario" SET "FrequenciaSemanal" = 8 WHERE "Id" = workout_a;
    RAISE EXCEPTION 'Invalid workout frequency was accepted';
  EXCEPTION WHEN check_violation THEN NULL;
  END;

  SET LOCAL ROLE authenticated;
  PERFORM set_config('request.jwt.claim.sub', user_a::text, true);
  PERFORM set_config('request.jwt.claims', json_build_object('sub', user_a, 'role', 'authenticated')::text, true);
  SELECT count(*), min("Id"::text)::uuid INTO row_count, visible_id FROM app."TreinosUsuario";
  IF row_count <> 1 OR visible_id <> workout_a THEN RAISE EXCEPTION 'User A isolation failed'; END IF;
  SELECT count(*), min("Id"::text)::uuid INTO row_count, visible_id FROM app."Usuarios";
  IF row_count <> 1 OR visible_id <> user_a THEN RAISE EXCEPTION 'Profile isolation failed'; END IF;
  FOREACH statement IN ARRAY ARRAY[
    'INSERT INTO app."TreinosUsuario" DEFAULT VALUES',
    'UPDATE app."TreinosUsuario" SET "Nome" = ''forbidden''',
    'DELETE FROM app."TreinosUsuario"',
    'SELECT * FROM app."Assinaturas"',
    'SELECT * FROM app."Pagamentos"',
    'SELECT * FROM app."BillingEvents"',
    'SELECT * FROM app."AuthSessions"',
    'SELECT * FROM app."Auditorias"',
    'SELECT * FROM public."__EFMigrationsHistory"'
  ] LOOP
    BEGIN
      EXECUTE statement;
      RAISE EXCEPTION 'Client operation unexpectedly allowed: %', statement;
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
  END LOOP;
  PERFORM set_config('request.jwt.claim.sub', user_b::text, true);
  PERFORM set_config('request.jwt.claims', json_build_object('sub', user_b, 'role', 'authenticated')::text, true);
  SELECT count(*), min("Id"::text)::uuid INTO row_count, visible_id FROM app."TreinosUsuario";
  IF row_count <> 1 OR visible_id <> workout_b THEN RAISE EXCEPTION 'User B isolation failed'; END IF;

  SET LOCAL ROLE anon;
  BEGIN
    PERFORM 1 FROM app."Usuarios";
    RAISE EXCEPTION 'Anonymous profile access unexpectedly allowed';
  EXCEPTION WHEN insufficient_privilege THEN NULL;
  END;
  RESET ROLE;
END $verify$;
ROLLBACK;
SELECT 'PASS: RLS A/B, denied client CRUD/private reads, soft delete, operation ID, FK, check constraints; test rows rolled back' AS verification;
