import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-docentes',
  imports: [CommonModule, FormsModule],
  templateUrl: './docentes.html',
  styleUrl: './docentes.scss'
})
export class Docentes {
  // Datos de ejemplo. No se conectan con la base de datos.
  docentes = [
    { id: 1, nombre: 'Ana Martínez', correo: 'ana.martinez@ues.edu.sv', departamento: 'Matemática', especialidad: 'Cálculo', activo: true },
    { id: 2, nombre: 'Roberto Pérez', correo: 'roberto.perez@ues.edu.sv', departamento: 'Informática', especialidad: 'Programación', activo: true },
    { id: 3, nombre: 'Lucía Hernández', correo: 'lucia.hernandez@ues.edu.sv', departamento: 'Física', especialidad: 'Mecánica', activo: false },
    { id: 4, nombre: 'Miguel Torres', correo: 'miguel.torres@ues.edu.sv', departamento: 'Informática', especialidad: 'Bases de datos', activo: true }
  ];

  busqueda = '';
  filtroEstado = 'Todos';
  editandoId = 0;
  mensaje = '';
  error = '';

  formulario = {
    nombre: '',
    correo: '',
    departamento: '',
    especialidad: ''
  };

  // Filtra la tabla por nombre, correo y estado.
  docentesFiltrados() {
    return this.docentes.filter(docente => {
      const texto = this.busqueda.toLowerCase();
      const coincideTexto = docente.nombre.toLowerCase().includes(texto) ||
        docente.correo.toLowerCase().includes(texto);
      const coincideEstado = this.filtroEstado === 'Todos' ||
        (this.filtroEstado === 'Activos' && docente.activo) ||
        (this.filtroEstado === 'Inactivos' && !docente.activo);
      return coincideTexto && coincideEstado;
    });
  }

  totalActivos() {
    return this.docentes.filter(docente => docente.activo).length;
  }

  limpiarFormulario() {
    this.formulario = { nombre: '', correo: '', departamento: '', especialidad: '' };
    this.editandoId = 0;
    this.error = '';
  }

  guardarDocente() {
    this.error = '';
    this.mensaje = '';

    const nombre = this.formulario.nombre.trim();
    const correo = this.formulario.correo.trim();
    const departamento = this.formulario.departamento;
    const especialidad = this.formulario.especialidad.trim();

    if (!nombre || !correo || !departamento || !especialidad) {
      this.error = 'Completa todos los campos del formulario.';
      return;
    }

    if (!correo.includes('@') || !correo.includes('.')) {
      this.error = 'Escribe un correo válido.';
      return;
    }

    const correoRepetido = this.docentes.some(docente =>
      docente.correo.toLowerCase() === correo.toLowerCase() &&
      docente.id !== this.editandoId
    );

    if (correoRepetido) {
      this.error = 'Ya existe un docente con ese correo.';
      return;
    }

    if (this.editandoId === 0) {
      const nuevoId = Math.max(0, ...this.docentes.map(docente => docente.id)) + 1;
      this.docentes.push({
        id: nuevoId, nombre, correo, departamento, especialidad, activo: true
      });
      this.mensaje = 'Docente agregado correctamente (solo demostración).';
    } else {
      const docente = this.docentes.find(docente => docente.id === this.editandoId);
      if (docente) {
        docente.nombre = nombre;
        docente.correo = correo;
        docente.departamento = departamento;
        docente.especialidad = especialidad;
        this.mensaje = 'Datos del docente actualizados (solo demostración).';
      }
    }

    this.limpiarFormulario();
  }

  editarDocente(docente: any) {
    this.editandoId = docente.id;
    this.formulario = {
      nombre: docente.nombre,
      correo: docente.correo,
      departamento: docente.departamento,
      especialidad: docente.especialidad
    };
    this.error = '';
    this.mensaje = '';
  }

  cambiarEstado(docente: any) {
    docente.activo = !docente.activo;
    this.mensaje = docente.activo
      ? 'Docente activado (solo demostración).'
      : 'Docente desactivado (solo demostración).';
  }
}
