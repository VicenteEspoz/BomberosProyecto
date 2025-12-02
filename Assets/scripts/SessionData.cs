using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SessionData
{
    // Campo estático que mantiene su valor aunque se carguen nuevas escenas.
    // Usamos 'int?' (nullable int) para permitir el valor null si no hay sesión activa.
    public static int? CurrentSessionId { get; set; } = null;

}
