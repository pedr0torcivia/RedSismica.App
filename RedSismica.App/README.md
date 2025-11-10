# 🌎 Proyecto Red Sísmica – Documentación Técnica y de Funcionamiento

## 1. Visión General del Sistema

**Red Sísmica** es una aplicación de escritorio desarrollada en **C# / .NET 6** con **Windows Forms**, que permite al **operador del Centro de Control de la Red Sísmica (CCRS)** revisar, validar y registrar resultados sobre **eventos sísmicos detectados automáticamente** por el sistema.

El objetivo principal es **automatizar la revisión manual de sismos auto-detectados**, manteniendo la trazabilidad de los cambios de estado de cada evento, siguiendo los requerimientos funcionales y no funcionales definidos por la **UTN-FRC / FRVM** en el **PPAI 2025**:contentReference[oaicite:0]{index=0}.

La aplicación implementa una **arquitectura en tres capas**, una **base de datos local SQLite (rs.db)** y un **modelo de dominio puro**, desacoplado de la infraestructura y la interfaz.

---

## 2. Arquitectura General del Sistema

### 🧩 Capas Principales

#### 1. `RedSismica.Core` — **Dominio Puro**

**Propósito:** Contiene la lógica de negocio independiente de UI o persistencia.  
**Patrones aplicados:** *State*, *GRASP (Controller, Expert)*, y principios **SOLID**.

**Componentes clave:**
- **Entidades:** `EventoSismico`, `SerieTemporal`, `MuestraSismica`, `Sismografo`, `CambioDeEstado`, `Empleado`, etc.
- **Modelo unidireccional:** las relaciones apuntan de padre a hijo, evitando referencias circulares.
- **Patrón State:** implementa los estados del evento (`Autodetectado`, `Bloqueado`, `Rechazado`, `Confirmado`, etc.) encapsulando el comportamiento en clases concretas.

> Ejemplo:  
> `EventoSismico.registrarEstadoBloqueado()` delega a `Autodetectado.registrarEstadoBloqueado()`, que cierra el estado actual y crea uno nuevo “Bloqueado”.

---

#### 2. `RedSismica.Infrastructure` — **Persistencia de Datos**

**Propósito:** Gestiona el almacenamiento de las entidades del dominio usando **Entity Framework Core 6.0** y **SQLite**.

**Componentes clave:**
- **`RedSismicaContext.cs`**: `DbContext` de EF Core.  
  - Define los `DbSet<>` y el mapeo mediante **shadow properties** para conservar el dominio puro sin Ids.
  - Configura las relaciones entre tablas (`EventoSismico`, `SerieTemporal`, `MuestraSismica`, etc.).
- **`EventoRepositoryEF.cs`**: implementa consultas sobre los eventos, usando `.ToList().Where(...)` para filtrar propiedades calculadas (ej. `EstadoActual.NombreEstado`).
- **`BulkTxtImporter.cs`**: inicializa la base local `rs.db` a partir de archivos `.txt` (carpeta `/import`) la primera vez que se ejecuta el programa.

---

#### 3. `RedSismica.App` — **Aplicación y UI (Windows Forms)**

**Propósito:** Provee la interfaz y la interacción con el usuario final.  
**Tipo:** Aplicación ejecutable `.exe` con *Dependency Injection* manual.

**Componentes clave:**
- **`Program.cs`**: punto de entrada. Configura EF Core, ejecuta `BulkTxtImporter.Run()`, crea el `ManejadorRegistrarRespuesta` e inyecta dependencias en la vista `PantallaNuevaRevision`.
- **`PantallaNuevaRevision.cs`**: clase *Boundary*.  
  No contiene lógica de negocio; solo delega eventos al Manejador y actualiza la interfaz (DataGridView, controles, mensajes, etc.).
- **`ManejadorRegistrarRespuesta.cs`**: *Controlador del caso de uso*.  
  Orquesta todo el flujo: obtiene eventos, bloquea, cambia estados, actualiza DB y coordina la UI.
- **`CU_GenerarSismograma.cs`**: servicio auxiliar que genera imágenes `.png` simuladas de sismogramas para cada estación.

---

## 3. Flujo del Caso de Uso  
### “Registrar Resultado de Revisión Manual” (CU23)

Este caso de uso fue asignado a los grupos impares según las **consignas oficiales del PPAI 2025**:contentReference[oaicite:1]{index=1}.  
A continuación se describe su ejecución en la aplicación.

---

### 🧭 Fase 1: Inicio y Carga de Datos
1. El operador abre la aplicación.  
2. `Program.cs` configura EF Core y ejecuta `BulkTxtImporter.Run()` para poblar `rs.db`.
3. Se abre `PantallaNuevaRevision`, ocultando los controles excepto el botón *Iniciar CU*.
4. El operador hace clic en *Iniciar* → la pantalla llama a `manejador.RegistrarNuevaRevision()`.

---

### 📋 Fase 2: Búsqueda y Muestra de Eventos
1. El manejador llama a `_repo.BuscarEventosAutodetectadosNoRevisados()`.
2. El repositorio trae todos los eventos y filtra en memoria los de estado `Autodetectado`.
3. Se ordenan por fecha y se muestra la grilla de eventos en la UI (`SolicitarSeleccionEvento()`).

---

### 🔒 Fase 3: Selección y Bloqueo del Evento
1. El operador selecciona un evento.
2. El manejador revierte bloqueos previos, si existen.
3. Llama a `eventoSeleccionado.registrarEstadoBloqueado()`, que:
   - Cierra el cambio de estado “Autodetectado”.
   - Crea uno nuevo “Bloqueado”.
   - Persiste los cambios (`_ctx.SaveChanges()`).
4. Se muestra el mensaje: *“El Evento ha sido Bloqueado…”*.

---

### 📊 Fase 4: Visualización de Detalles
1. El manejador ejecuta `buscarDetallesEventoSismico()`:
   - Obtiene `Alcance`, `Clasificación`, `Origen` y series temporales con sus muestras.
   - Genera un texto detallado con las características del evento.
2. Llama a `generarSismograma()`, que produce un `.png` mostrado en la interfaz.
3. Se habilitan las acciones posibles: *Confirmar*, *Rechazar*, *Solicitar revisión*.

---

### ✅ Fase 5: Acción Final
1. El operador elige **“Rechazar Evento”**.
2. El manejador valida datos y ejecuta `eventoSeleccionado.rechazar(fecha, responsable)`:
   - Cierra “Bloqueado”.
   - Crea el nuevo estado “Rechazado”.
   - Persiste los cambios.
3. Se genera el resumen final con el historial de cambios:
