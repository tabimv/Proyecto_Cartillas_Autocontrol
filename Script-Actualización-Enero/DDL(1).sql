-- Generado por Oracle SQL Developer Data Modeler 19.4.0.350.1424
--   en:        2023-11-09 10:03:11 CLST
--   sitio:      SQL Server 2012
--   tipo:      SQL Server 2012



CREATE TABLE ACTIVIDAD 
    (
     actividad_id INTEGER IDENTITY(1, 1) NOT NULL , 
     codigo_actividad VARCHAR (10) NOT NULL , 
     nombre_actividad VARCHAR (100) NOT NULL , 
     estado CHAR (1) NOT NULL , 
     OBRA_obra_id INTEGER NOT NULL 
    )
GO

ALTER TABLE ACTIVIDAD ADD CONSTRAINT ACTIVIDAD_PK PRIMARY KEY CLUSTERED (actividad_id)
     WITH (
     ALLOW_PAGE_LOCKS = ON , 
     ALLOW_ROW_LOCKS = ON )
GO

CREATE TABLE CARTILLA 
    (
     cartilla_id INTEGER IDENTITY(1, 1) NOT NULL , 
     fecha DATE NOT NULL , 
     observaciones VARCHAR (200) , 
	 ruta_pdf VARCHAR (MAX),
     OBRA_obra_id INTEGER NOT NULL , 
     ACTIVIDAD_actividad_id INTEGER NOT NULL , 
     ESTADO_FINAL_estado_final_id INTEGER NOT NULL 
    )
GO

ALTER TABLE CARTILLA ADD CONSTRAINT CARTILLA_PK PRIMARY KEY CLUSTERED (cartilla_id)
     WITH (
     ALLOW_PAGE_LOCKS = ON , 
     ALLOW_ROW_LOCKS = ON )
GO

CREATE TABLE COMUNA 
    (
     comuna_id INTEGER IDENTITY(1, 1) NOT NULL , 
     nombre_comuna VARCHAR (100) NOT NULL , 
     REGION_region_id INTEGER NOT NULL 
    )
GO

ALTER TABLE COMUNA ADD CONSTRAINT COMUNA_PK PRIMARY KEY CLUSTERED (comuna_id)
     WITH (
     ALLOW_PAGE_LOCKS = ON , 
     ALLOW_ROW_LOCKS = ON )
GO

CREATE TABLE DETALLE_CARTILLA 
    (
     detalle_cartilla_id INTEGER IDENTITY(1, 1) NOT NULL , 
     estado_otec BIT NOT NULL , 
     estado_ito BIT NOT NULL , 
	 estado_supv BIT NOT NULL , 
     ITEM_VERIF_item_verif_id INTEGER NOT NULL , 
     CARTILLA_cartilla_id INTEGER NOT NULL , 
     INMUEBLE_inmueble_id INTEGER NOT NULL 
    )
GO

ALTER TABLE DETALLE_CARTILLA ADD CONSTRAINT DETALLE_CARTILLA_PK PRIMARY KEY CLUSTERED (detalle_cartilla_id)
     WITH (
     ALLOW_PAGE_LOCKS = ON , 
     ALLOW_ROW_LOCKS = ON )
GO

CREATE TABLE ESTADO_FINAL 
    (
     estado_final_id INTEGER IDENTITY(1, 1) NOT NULL , 
     estado CHAR (4) NOT NULL , 
     descripcion VARCHAR (100) NOT NULL 
    )
GO

ALTER TABLE ESTADO_FINAL ADD CONSTRAINT ESTADO_FINAL_PK PRIMARY KEY CLUSTERED (estado_final_id)
     WITH (
     ALLOW_PAGE_LOCKS = ON , 
     ALLOW_ROW_LOCKS = ON )
GO

CREATE TABLE INMUEBLE 
    (
     inmueble_id INTEGER IDENTITY(1, 1) NOT NULL , 
	 codigo_inmueble VARCHAR (5) NOT NULL,
     tipo_inmueble VARCHAR (30) NOT NULL , 
     OBRA_obra_id INTEGER NOT NULL 
    )
GO

ALTER TABLE INMUEBLE ADD CONSTRAINT INMUEBLE_PK PRIMARY KEY CLUSTERED (inmueble_id)
     WITH (
     ALLOW_PAGE_LOCKS = ON , 
     ALLOW_ROW_LOCKS = ON )
GO

CREATE TABLE ITEM_VERIF 
    (
     item_verif_id INTEGER IDENTITY(1, 1) NOT NULL , 
     elemento_verificacion VARCHAR (200) NOT NULL , 
     label VARCHAR (2) NOT NULL , 
     ACTIVIDAD_actividad_id INTEGER NOT NULL 
    )
GO

ALTER TABLE ITEM_VERIF ADD CONSTRAINT ITEM_VERIF_PK PRIMARY KEY CLUSTERED (item_verif_id)
     WITH (
     ALLOW_PAGE_LOCKS = ON , 
     ALLOW_ROW_LOCKS = ON )
GO

CREATE TABLE OBRA 
    (
     obra_id INTEGER IDENTITY(1, 1) NOT NULL , 
     nombre_obra VARCHAR (100) NOT NULL , 
     direccion VARCHAR (100) NOT NULL , 
     COMUNA_comuna_id INTEGER NOT NULL 
    )
GO

ALTER TABLE OBRA ADD CONSTRAINT OBRA_PK PRIMARY KEY CLUSTERED (obra_id)
     WITH (
     ALLOW_PAGE_LOCKS = ON , 
     ALLOW_ROW_LOCKS = ON )
GO

CREATE TABLE PERFIL 
    (
     perfil_id INTEGER IDENTITY(1, 1) NOT NULL , 
     rol VARCHAR (50) NOT NULL 
    )
GO

ALTER TABLE PERFIL ADD CONSTRAINT PERFIL_PK PRIMARY KEY CLUSTERED (perfil_id)
     WITH (
     ALLOW_PAGE_LOCKS = ON , 
     ALLOW_ROW_LOCKS = ON )
GO

CREATE TABLE PERSONA 
    (
     rut VARCHAR (12) NOT NULL , 
     nombre VARCHAR (50) NOT NULL , 
     apeliido_paterno VARCHAR (50) NOT NULL , 
     apellido_materno VARCHAR (50) , 
     correo VARCHAR (100) NOT NULL 
    )
GO

ALTER TABLE PERSONA ADD CONSTRAINT PERSONA_PK PRIMARY KEY CLUSTERED (rut)
     WITH (
     ALLOW_PAGE_LOCKS = ON , 
     ALLOW_ROW_LOCKS = ON )
GO

CREATE TABLE REGION 
    (
     region_id INTEGER IDENTITY(1, 1) NOT NULL , 
     nombre_region VARCHAR (100) NOT NULL 
    )
GO

ALTER TABLE REGION ADD CONSTRAINT REGION_PK PRIMARY KEY CLUSTERED (region_id)
     WITH (
     ALLOW_PAGE_LOCKS = ON , 
     ALLOW_ROW_LOCKS = ON )
GO

CREATE TABLE RESPONSABLE 
    (
     responsable_id INTEGER IDENTITY(1, 1) NOT NULL , 
     cargo VARCHAR (100) NOT NULL , 
     OBRA_obra_id INTEGER NOT NULL , 
     PERSONA_rut VARCHAR (12) NOT NULL 
    )
GO 

    


CREATE UNIQUE NONCLUSTERED INDEX 
    RESPONSABLE__IDX ON RESPONSABLE 
    ( 
     PERSONA_rut 
    ) 
GO

ALTER TABLE RESPONSABLE ADD CONSTRAINT RESPONSABLE_PK PRIMARY KEY CLUSTERED (responsable_id)
     WITH (
     ALLOW_PAGE_LOCKS = ON , 
     ALLOW_ROW_LOCKS = ON )
GO

CREATE TABLE USUARIO 
    (
     usuario_id INTEGER IDENTITY(1, 1) NOT NULL , 
     contraseña VARCHAR (50) NOT NULL , 
     PERFIL_perfil_id INTEGER NOT NULL , 
     OBRA_obra_id INTEGER NOT NULL , 
     PERSONA_rut VARCHAR (12) NOT NULL 
    )
GO 

    


CREATE UNIQUE NONCLUSTERED INDEX 
    USUARIO__IDX ON USUARIO 
    ( 
     PERSONA_rut 
    ) 
GO

ALTER TABLE USUARIO ADD CONSTRAINT USUARIO_PK PRIMARY KEY CLUSTERED (usuario_id)
     WITH (
     ALLOW_PAGE_LOCKS = ON , 
     ALLOW_ROW_LOCKS = ON )
GO

ALTER TABLE ACTIVIDAD 
    ADD CONSTRAINT ACTIVIDAD_OBRA_FK FOREIGN KEY 
    ( 
     OBRA_obra_id
    ) 
    REFERENCES OBRA 
    ( 
     obra_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE CARTILLA 
    ADD CONSTRAINT CARTILLA_ACTIVIDAD_FK FOREIGN KEY 
    ( 
     ACTIVIDAD_actividad_id
    ) 
    REFERENCES ACTIVIDAD 
    ( 
     actividad_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE CARTILLA 
    ADD CONSTRAINT CARTILLA_ESTADO_FINAL_FK FOREIGN KEY 
    ( 
     ESTADO_FINAL_estado_final_id
    ) 
    REFERENCES ESTADO_FINAL 
    ( 
     estado_final_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE CARTILLA 
    ADD CONSTRAINT CARTILLA_OBRA_FK FOREIGN KEY 
    ( 
     OBRA_obra_id
    ) 
    REFERENCES OBRA 
    ( 
     obra_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE COMUNA 
    ADD CONSTRAINT COMUNA_REGION_FK FOREIGN KEY 
    ( 
     REGION_region_id
    ) 
    REFERENCES REGION 
    ( 
     region_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE DETALLE_CARTILLA 
    ADD CONSTRAINT DETALLE_CARTILLA_CARTILLA_FK FOREIGN KEY 
    ( 
     CARTILLA_cartilla_id
    ) 
    REFERENCES CARTILLA 
    ( 
     cartilla_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE DETALLE_CARTILLA 
    ADD CONSTRAINT DETALLE_CARTILLA_INMUEBLE_FK FOREIGN KEY 
    ( 
     INMUEBLE_inmueble_id
    ) 
    REFERENCES INMUEBLE 
    ( 
     inmueble_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE DETALLE_CARTILLA 
    ADD CONSTRAINT DETALLE_CARTILLA_ITEM_VERIF_FK FOREIGN KEY 
    ( 
     ITEM_VERIF_item_verif_id
    ) 
    REFERENCES ITEM_VERIF 
    ( 
     item_verif_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE INMUEBLE 
    ADD CONSTRAINT INMUEBLE_OBRA_FK FOREIGN KEY 
    ( 
     OBRA_obra_id
    ) 
    REFERENCES OBRA 
    ( 
     obra_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE ITEM_VERIF 
    ADD CONSTRAINT ITEM_VERIF_ACTIVIDAD_FK FOREIGN KEY 
    ( 
     ACTIVIDAD_actividad_id
    ) 
    REFERENCES ACTIVIDAD 
    ( 
     actividad_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE OBRA 
    ADD CONSTRAINT OBRA_COMUNA_FK FOREIGN KEY 
    ( 
     COMUNA_comuna_id
    ) 
    REFERENCES COMUNA 
    ( 
     comuna_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE RESPONSABLE 
    ADD CONSTRAINT RESPONSABLE_OBRA_FK FOREIGN KEY 
    ( 
     OBRA_obra_id
    ) 
    REFERENCES OBRA 
    ( 
     obra_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE RESPONSABLE 
    ADD CONSTRAINT RESPONSABLE_PERSONA_FK FOREIGN KEY 
    ( 
     PERSONA_rut
    ) 
    REFERENCES PERSONA 
    ( 
     rut 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE USUARIO 
    ADD CONSTRAINT USUARIO_OBRA_FK FOREIGN KEY 
    ( 
     OBRA_obra_id
    ) 
    REFERENCES OBRA 
    ( 
     obra_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE USUARIO 
    ADD CONSTRAINT USUARIO_PERFIL_FK FOREIGN KEY 
    ( 
     PERFIL_perfil_id
    ) 
    REFERENCES PERFIL 
    ( 
     perfil_id 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO

ALTER TABLE USUARIO 
    ADD CONSTRAINT USUARIO_PERSONA_FK FOREIGN KEY 
    ( 
     PERSONA_rut
    ) 
    REFERENCES PERSONA 
    ( 
     rut 
    ) 
    ON DELETE NO ACTION 
    ON UPDATE NO ACTION 
GO



-- Informe de Resumen de Oracle SQL Developer Data Modeler: 
-- 
-- CREATE TABLE                            13
-- CREATE INDEX                             2
-- ALTER TABLE                             29
-- CREATE VIEW                              0
-- ALTER VIEW                               0
-- CREATE PACKAGE                           0
-- CREATE PACKAGE BODY                      0
-- CREATE PROCEDURE                         0
-- CREATE FUNCTION                          0
-- CREATE TRIGGER                           0
-- ALTER TRIGGER                            0
-- CREATE DATABASE                          0
-- CREATE DEFAULT                           0
-- CREATE INDEX ON VIEW                     0
-- CREATE ROLLBACK SEGMENT                  0
-- CREATE ROLE                              0
-- CREATE RULE                              0
-- CREATE SCHEMA                            0
-- CREATE SEQUENCE                          0
-- CREATE PARTITION FUNCTION                0
-- CREATE PARTITION SCHEME                  0
-- 
-- DROP DATABASE                            0
-- 
-- ERRORS                                   0
-- WARNINGS                                 0
