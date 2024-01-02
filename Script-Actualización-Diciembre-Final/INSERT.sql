-- Insertar datos en la tabla Perfil
INSERT INTO perfil (rol)
VALUES ('Administrador'),
       ('Consulta'),
	   ('OTEC'),
	   ('ITO');

-- Insertar datos en la tabla Región
INSERT INTO REGION (nombre_region)
VALUES
    ('Región de Arica y Parinacota'),
    ('Región de Tarapacá'),
    ('Región de Antofagasta'),
    ('Región de Atacama'),
    ('Región de Coquimbo'),
    ('Región de Valparaíso'),
    ('Región Metropolitana de Santiago'),
    ('Región del Libertador General Bernardo O''Higgins'),
    ('Región del Maule'),
    ('Región de Ñuble'),
    ('Región del Biobío'),
    ('Región de La Araucanía'),
    ('Región de Los Ríos'),
    ('Región de Los Lagos'),
    ('Región Aysén del General Carlos Ibáñez del Campo'),
    ('Región de Magallanes y de la Antártica Chilena');


-- Insertar datos en la tabla COMUNA para la Región de Arica y Parinacota
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Arica', 1),
    ('Camarones', 1),
    ('Putre', 1),
    ('General Lagos', 1);

-- Insertar datos en la tabla COMUNA para la Región de Tarapacá
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Iquique', 2),
    ('Alto Hospicio', 2),
    ('Pozo Almonte', 2),
    ('Camiña', 2),
    ('Colchane', 2),
    ('Huara', 2),
	('Pica', 2);

-- Insertar datos en la tabla COMUNA para la Región de Antofagasta
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Antofagasta', 3),
    ('Mejillones', 3),
    ('Sierra Gorda', 3),
    ('Taltal', 3),
    ('Calama', 3),
    ('Ollagüe', 3),
    ('San Pedro de Atacama', 3),
    ('María Elena', 3);

-- Insertar datos en la tabla COMUNA para la Región de Atacama
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Copiapó', 4),
    ('Caldera', 4),
    ('Tierra Amarilla', 4),
    ('Chañaral', 4),
    ('Diego de Almagro', 4),
    ('Vallenar', 4),
    ('Alto del Carmen', 4),
    ('Freirina', 4),
    ('Huasco', 4);


-- Insertar datos en la tabla COMUNA para la Región de Coquimbo
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('La Serena', 5),
    ('Coquimbo', 5),
    ('Andacollo', 5),
    ('La Higuera', 5),
    ('Paiguano', 5),
    ('Vicuña', 5),
    ('Illapel', 5),
    ('Canela', 5),
    ('Los Vilos', 5),
    ('Salamanca', 5);

-- Insertar datos en la tabla COMUNA para la Región de Valparaíso
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Valparaíso', 6),
    ('Viña del Mar', 6),
    ('Concón', 6),
    ('Quilpué', 6),
    ('Villa Alemana', 6),
    ('Quintero', 6),
    ('Puchuncaví', 6),
    ('Casablanca', 6),
    ('Juan Fernández', 6),
    ('Isla de Pascua', 6);

-- Insertar datos en la tabla COMUNA para la Región Metropolitana de Santiago
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Alhué', 7),
    ('Buin', 7),
    ('Calera de Tango', 7),
    ('Cerrillos', 7),
    ('Cerro Navia', 7),
    ('Colina', 7),
    ('Conchalí', 7),
    ('Curacaví', 7),
    ('El Bosque', 7),
    ('El Monte', 7),
    ('Estación Central', 7),
    ('Huechuraba', 7),
    ('Independencia', 7),
    ('Isla de Maipo', 7),
    ('La Cisterna', 7),
    ('La Florida', 7),
    ('La Granja', 7),
    ('Lampa', 7),
    ('La Pintana', 7),
    ('La Reina', 7),
    ('Las Condes', 7),
    ('Lo Barnechea', 7),
    ('Lo Espejo', 7),
    ('Lo Prado', 7),
    ('Macul', 7),
    ('Maipú', 7),
    ('María Pinto', 7),
    ('Melipilla', 7),
    ('Ñuñoa', 7),
    ('Padre Hurtado', 7),
    ('Paine', 7),
    ('Pedro Aguirre Cerda', 7),
    ('Peñaflor', 7),
    ('Peñalolén', 7),
    ('Pirque', 7),
    ('Providencia', 7),
    ('Pudahuel', 7),
    ('Puente Alto', 7),
    ('Quilicura', 7),
    ('Quinta Normal', 7),
    ('Recoleta', 7),
    ('Renca', 7),
    ('San Bernardo', 7),
    ('San Joaquín', 7),
    ('San José de Maipo', 7),
    ('San Miguel', 7),
    ('San Pedro', 7),
    ('San Ramón', 7),
    ('Santiago', 7),
    ('Talagante', 7),
    ('Tiltil', 7),
    ('Vitacura', 7);


-- Insertar datos en la tabla COMUNA para la Región del Libertador General Bernardo O'Higgins
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Rancagua', 8),
    ('Machalí', 8),
    ('Graneros', 8),
    ('San Fernando', 8),
    ('Chimbarongo', 8),
    ('Peumo', 8),
    ('San Vicente de Tagua Tagua', 8),
    ('Pichilemu', 8),
    ('Las Cabras', 8),
    ('Palmilla', 8);

-- Insertar datos en la tabla COMUNA para la Región del Maule
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Talca', 9),
    ('Curicó', 9),
    ('Linares', 9),
    ('Constitución', 9),
    ('Cauquenes', 9),
    ('Molina', 9),
    ('Teno', 9),
    ('San Clemente', 9),
    ('Maule', 9),
    ('Pelarco', 9);

-- Insertar datos en la tabla COMUNA para la Región de Ñuble
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Chillán', 10),
    ('Chillán Viejo', 10),
    ('San Carlos', 10),
    ('Bulnes', 10),
    ('Quirihue', 10),
    ('Yungay', 10),
    ('Ñiquén', 10),
    ('Cobquecura', 10),
    ('Coelemu', 10),
    ('Portezuelo', 10);

-- Insertar datos en la tabla COMUNA para la Región del Biobío
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Alto Biobío', 11),
    ('Antuco', 11),
    ('Arauco', 11),
    ('Cabrero', 11),
    ('Cañete', 11),
    ('Chiguayante', 11),
    ('Concepción', 11),
    ('Contulmo', 11),
    ('Coronel', 11),
    ('Curanilahue', 11),
    ('Florida', 11),
    ('Hualpén', 11),
    ('Hualqui', 11),
    ('Laja', 11),
    ('Lebu', 11),
    ('Los Alamos', 11),
    ('Los Angeles', 11),
    ('Lota', 11),
    ('Mulchén', 11),
    ('Nacimiento', 11),
    ('Negrete', 11),
    ('Penco', 11),
    ('Quilaco', 11),
    ('Quilleco', 11),
    ('San Pedro de la Paz', 11),
    ('San Rosendo', 11),
    ('Santa Bárbara', 11),
    ('Santa Juana', 11),
    ('Talcahuano', 11),
    ('Tirúa', 11),
    ('Tomé', 11),
    ('Tucapel', 11),
    ('Yumbel', 11);

-- Insertar datos en la tabla COMUNA para la Región de La Araucanía
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Temuco', 12),
    ('Padre Las Casas', 12),
    ('Puente Alto', 12),
    ('Pitrufquén', 12),
    ('Villarrica', 12),
    ('Victoria', 12),
    ('Angol', 12),
    ('Loncoche', 12),
    ('Lautaro', 12),
    ('Curarrehue', 12);

-- Insertar datos en la tabla COMUNA para la Región de Los Ríos
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Valdivia', 13),
    ('La Unión', 13),
    ('Paillaco', 13),
    ('Los Lagos', 13),
    ('Río Bueno', 13),
    ('Lanco', 13),
    ('Mariquina', 13),
    ('Panguipulli', 13),
    ('Futrono', 13),
    ('Corral', 13);

-- Insertar datos en la tabla COMUNA para la Región de Los Lagos
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Puerto Montt', 14),
    ('Osorno', 14),
    ('Puerto Varas', 14),
    ('Castro', 14),
    ('Ancud', 14),
    ('Quellón', 14),
    ('Calbuco', 14),
    ('Frutillar', 14),
    ('Llanquihue', 14),
    ('Purranque', 14);

-- Insertar datos en la tabla COMUNA para la Región de Aysén del General Carlos Ibáñez del Campo
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Coyhaique', 15),
    ('Puerto Aysén', 15),
    ('Chile Chico', 15),
    ('Cochrane', 15),
    ('Cisnes', 15),
    ('Guaitecas', 15),
    ('Lago Verde', 15),
    ('O''Higgins', 15),
    ('Río Ibáñez', 15),
    ('Tortel', 15);

-- Insertar datos en la tabla COMUNA para la Región de Magallanes y de la Antártica Chilena
INSERT INTO COMUNA (nombre_comuna, REGION_region_id)
VALUES
    ('Punta Arenas', 16),
    ('Puerto Natales', 16),
    ('Porvenir', 16),
    ('Cabo de Hornos (Ex Navarino)', 16),
    ('Río Verde', 16),
    ('Primavera', 16),
    ('Timaukel', 16),
    ('Antártica', 16);


-- Insertar datos en la tabla Obra
INSERT INTO obra (nombre_obra, direccion, comuna_comuna_id)
VALUES ( 'Oficina Central', 'Las Margaritas 1925', 155);

-- Insertar datos en la tabla Persona
INSERT INTO persona (rut, nombre, apeliido_paterno, apellido_materno, correo)
VALUES ('20928183-k', 'Tabita', 'Melo', 'Vera', 'ta.melo@duocuc.cl');

-- Insertar datos en la tabla Usuario
INSERT INTO usuario (contraseña, PERFIL_perfil_id, obra_obra_id, persona_rut)
VALUES ('123', 1, 1, '20928183-k');

-- Insertar datos en la tabla Estado Final
INSERT INTO estado_final (estado, descripcion)
VALUES ('VB', 'Aprobada'),
       ('R', 'Rechazada'),
       ('EP', 'En proceso');




