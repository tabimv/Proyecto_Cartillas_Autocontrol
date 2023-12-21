-- Insertar datos en la tabla Perfil
INSERT INTO perfil (rol)
VALUES ('Administrador'),
       ('Consulta'),
	   ('OTEC'),
	   ('ITO');

-- Insertar datos en la tabla Región
INSERT INTO region ( nombre_region)
VALUES ('Región del Biobío');


-- Insertar datos en la tabla Comuna
INSERT INTO comuna (nombre_comuna, region_region_id)
VALUES ( 'Concepción', 1 );

-- Insertar datos en la tabla Obra
INSERT INTO obra (nombre_obra, direccion, comuna_comuna_id)
VALUES ( 'Proyecto Peumo', 'Camino a Nonguen n°2230, Lote A Valle Nonguen', 1);

-- Insertar datos en la tabla Persona
INSERT INTO persona (rut, nombre, apeliido_paterno, apellido_materno, correo)
VALUES ('20928183-k', 'Tabita', 'Melo', 'Vera', 'ta.melo@duocuc.cl');

-- Insertar datos en la tabla Usuario
INSERT INTO usuario (contraseña, PERFIL_perfil_id, obra_obra_id, persona_rut)
VALUES ('admin123', 1, 1, '20928183-k');

-- Insertar datos en la tabla Estado Final
INSERT INTO estado_final (estado, descripcion)
VALUES ('VB', 'Aprobada'),
       ('R', 'Rechazada'),
       ('EP', 'En proceso');




