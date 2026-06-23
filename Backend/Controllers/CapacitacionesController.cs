using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.DataContext;
using Service.Models;
using Backend.ExtensionMethods;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CapacitacionesController : ControllerBase
    {
        private readonly AgoraContext _context;

        public CapacitacionesController(AgoraContext context)
        {
            _context = context;
        }

        // GET: api/Capacitaciones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Capacitacion>>> GetCapacitaciones([FromQuery] string? filter = "")
        {
            return await _context.Capacitaciones
                            .Include(c=> c.TiposDeInscripciones).ThenInclude(t=> t.TipoInscripcion)
                            .Include(c=> c.Inscripciones).ThenInclude(i=>i.Usuario)
                            .Include(c=> c.Inscripciones).ThenInclude(i=>i.UsuarioCobro)
                            .Include(c=> c.Inscripciones).ThenInclude(i=>i.TipoInscripcion)
                            .Where(c => c.Nombre.Contains(filter, StringComparison.OrdinalIgnoreCase)
                                || c.Detalle.Contains(filter, StringComparison.OrdinalIgnoreCase)
                                || c.Ponente.Contains(filter, StringComparison.OrdinalIgnoreCase))
                            .AsNoTracking()
                            .ToListAsync();
        }

        [HttpGet("abiertas")]
        public async Task<ActionResult<IEnumerable<Capacitacion>>> GetCapacitacionesAbiertas([FromQuery] string? filter = "")
        {
            return await _context.Capacitaciones
                                .Include(c => c.TiposDeInscripciones).ThenInclude(t => t.TipoInscripcion)
                                .Include(c => c.Inscripciones).ThenInclude(i => i.Usuario)
                                .Include(c => c.Inscripciones).ThenInclude(i => i.UsuarioCobro)
                                .Include(c => c.Inscripciones).ThenInclude(i => i.TipoInscripcion)
                                .Where(c => c.InscripcionAbierta && 
                                        (c.Nombre.Contains(filter)
                                            || c.Detalle.Contains(filter) 
                                            || c.Ponente.Contains(filter)))
                                .AsNoTracking()
                                .ToListAsync();
        }
        [HttpGet("futuras")]
        public async Task<ActionResult<IEnumerable<Capacitacion>>> GetCapacitacionesFuturas([FromQuery] string? filter = "")
        {
            return await _context.Capacitaciones
                                .Include(c => c.TiposDeInscripciones).ThenInclude(t => t.TipoInscripcion)
                                .Include(c => c.Inscripciones).ThenInclude(i => i.Usuario)
                                .Include(c => c.Inscripciones).ThenInclude(i => i.UsuarioCobro)
                                .Include(c => c.Inscripciones).ThenInclude(i => i.TipoInscripcion)
                                .Where(c => !c.InscripcionAbierta 
                                        && c.FechaHora.Date > DateTime.Now.Date 
                                        && (c.Nombre.Contains(filter) 
                                            || c.Detalle.Contains(filter) 
                                            || c.Ponente.Contains(filter)))
                                .AsNoTracking()
                                .ToListAsync();
        }

        // GET: api/Capacitaciones
        [HttpGet("deleteds/")]
        public async Task<ActionResult<IEnumerable<Capacitacion>>> GetCapacitacionesDeleteds()
        {
            return await _context.Capacitaciones.AsNoTracking()
                                .IgnoreQueryFilters().Where(c => c.IsDeleted).ToListAsync();
        }

        // GET: api/Capacitaciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Capacitacion>> GetCapacitacion(int id)
        {
            var capacitacion = await _context.Capacitaciones.FindAsync(id);

            if (capacitacion == null)
            {
                return NotFound();
            }

            return capacitacion;
        }

        // PUT: api/Capacitaciones/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCapacitacion(int id, Capacitacion capacitacion)
        {
            if (id != capacitacion.Id)
            {
                return BadRequest();
            }
            _context.Entry(capacitacion).State = EntityState.Modified;

            #region Manejo de ICollection TiposDeInscripciones
            // Attach las entidades TipoInscripcion para que no intente guardarlas nuevamente
            foreach (var tipoInscripcionCapacitacion in capacitacion.TiposDeInscripciones)
            {
                _context.TryAttach(tipoInscripcionCapacitacion.TipoInscripcion);
            }

            var capacitacionExistente = await _context.Capacitaciones
                                                .Include(c => c.TiposDeInscripciones)
                                                .Include(c => c.Inscripciones)
                                                .AsNoTracking()
                                                .FirstOrDefaultAsync(c => c.Id == capacitacion.Id);
            if (capacitacionExistente == null)
            {
                return NotFound("No se encontró la capacitación que se intentaba modificar");
            }
            var tipodeInscripcionesAEliminar = capacitacionExistente.TiposDeInscripciones
                                                .Where(t => !capacitacion.TiposDeInscripciones
                                                .Any(ti => ti.Id == t.Id))
                                                .ToList();
            foreach (var tipoInscripcionCapacitacion in tipodeInscripcionesAEliminar)
            {
                _context.TryAttach(tipoInscripcionCapacitacion.TipoInscripcion);
                tipoInscripcionCapacitacion.Capacitacion = null;
                _context.TiposInscripcionesCapacitaciones.Remove(tipoInscripcionCapacitacion);
            }

            var tiposDeInscripcionesAAgregar = capacitacion.TiposDeInscripciones
                                                .Where(ti => !capacitacionExistente.TiposDeInscripciones
                                                .Any(t => t.Id == ti.Id))
                                                .ToList();

            foreach (var tipoInscripcionCapacitacion in tiposDeInscripcionesAAgregar)
            {
                _context.TryAttach(tipoInscripcionCapacitacion.TipoInscripcion);
                _context.TiposInscripcionesCapacitaciones.Add(tipoInscripcionCapacitacion);
            }
            #endregion
            #region Manejo de ICollection Inscripciones
            // Attach las entidades Usuario y UsuarioCobro para que no intente guardarlas nuevamente
            foreach (var inscripcion in capacitacion.Inscripciones)
            {
                inscripcion.Usuario = null;
                inscripcion.UsuarioCobro = null;
                inscripcion.Capacitacion = null;
                inscripcion.TipoInscripcion = null; // Evitar duplicados
            }

            
            var inscripcionesAEliminar = capacitacionExistente.Inscripciones
                                                .Where(t => !capacitacion.Inscripciones
                                                .Any(ti => ti.Id == t.Id))
                                                .ToList();
            foreach (var inscripcion1 in inscripcionesAEliminar)
            {
                inscripcion1.Usuario = null;
                inscripcion1.UsuarioCobro = null;
                inscripcion1.TipoInscripcion = null;
                inscripcion1.Capacitacion = null;
                _context.Inscripciones.Remove(inscripcion1);
            }

            var inscripcionesAAgregar = capacitacion.Inscripciones
                                                .Where(ti => !capacitacionExistente.Inscripciones
                                                .Any(t => t.Id == ti.Id))
                                                .ToList();

            foreach (var inscripcion2 in inscripcionesAAgregar)
            {
                inscripcion2.Usuario = null;
                inscripcion2.UsuarioCobro = null;
                inscripcion2.TipoInscripcion = null;
                inscripcion2.Capacitacion = null;
                _context.Inscripciones.Add(inscripcion2);
            }
            #endregion



            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException e)
            {
                if (!CapacitacionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw new Exception(e.Message);
                }
            }

            return NoContent();
        }

        // POST: api/Capacitaciones
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Capacitacion>> PostCapacitacion(Capacitacion capacitacion)
        {
            foreach(var tipoInscripcionCapacitacion in capacitacion.TiposDeInscripciones)
            {
                _context.Attach(tipoInscripcionCapacitacion.TipoInscripcion);
            }
            _context.Capacitaciones.Add(capacitacion);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCapacitacion", new { id = capacitacion.Id }, capacitacion);
        }

        // DELETE: api/Capacitaciones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCapacitacion(int id)
        {
            var capacitacion = await _context.Capacitaciones.FindAsync(id);
            if (capacitacion == null)
            {
                return NotFound();
            }
            capacitacion.IsDeleted = true; // Soft delete
            _context.Capacitaciones.Update(capacitacion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Capacitaciones/restore/5
        [HttpPut("restore/{id}")]
        public async Task<IActionResult> RestoreCapacitacion(int id)
        {
            var capacitacion = await _context.Capacitaciones.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id.Equals(id));
            if (capacitacion == null)
            {
                return NotFound();
            }
            capacitacion.IsDeleted = false; // Soft restore
            _context.Capacitaciones.Update(capacitacion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CapacitacionExists(int id)
        {
            return _context.Capacitaciones.Any(e => e.Id == id);
        }
    }
}
