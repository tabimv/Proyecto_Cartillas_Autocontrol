DROP TRIGGER trg_actualizar_estado_actividad
--*****************************************************************************************************
CREATE TRIGGER trg_actualizar_estado_actividad
ON CARTILLA
AFTER UPDATE
AS
BEGIN
    -- Verificar si se actualizó el campo ESTADO_FINAL_estado_final_id
    IF UPDATE(ESTADO_FINAL_estado_final_id)
    BEGIN
        -- Actualizar el estado de la ACTIVIDAD según el valor de ESTADO_FINAL_estado_final_id
        UPDATE ACTIVIDAD
        SET estado = CASE 
                        WHEN i.ESTADO_FINAL_estado_final_id = 1 THEN 'B'
						WHEN i.ESTADO_FINAL_estado_final_id = 2 THEN 'A'
						WHEN i.ESTADO_FINAL_estado_final_id = 3 THEN 'A'
						ELSE 'A'
                    END
        FROM ACTIVIDAD a
        INNER JOIN inserted i ON a.actividad_id = i.ACTIVIDAD_actividad_id
        WHERE i.ESTADO_FINAL_estado_final_id IN (1, 2, 3); -- Asegurarse de manejar otros valores de ESTADO_FINAL_estado_final_id si es necesario
    END
END;



