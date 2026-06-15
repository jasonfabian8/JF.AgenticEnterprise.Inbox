import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { emailApi, type AttachmentIngestDto } from '@/lib/api/client'
import { cn } from '@/lib/utils'

// ── Sample templates ──────────────────────────────────────────────────────────

interface EmailTemplate {
  senderName: string
  senderEmail: string
  subject: string
  body: string
  attachments: AttachmentIngestDto[]
}

const TEMPLATES: Record<string, EmailTemplate> = {
  Invoice: {
    senderName: 'Accounts Payable — Proveedor Global S.A.',
    senderEmail: 'cuentas@proveedorglobal.com',
    subject: 'Factura #PG-2024-00847 — Servicios de Consultoría — Marzo 2024',
    body: `Estimado equipo de finanzas,

Adjuntamos la factura correspondiente a los servicios de consultoría prestados durante el mes de marzo de 2024.

Detalles de la factura:
  Número de factura:  PG-2024-00847
  Fecha de emisión:   15/03/2024
  Fecha de vencimiento: 15/04/2024
  Proveedor:          Proveedor Global S.A.
  Concepto:           Consultoría estratégica — 120 horas
  Subtotal:           USD 18,000.00
  IVA (16%):          USD 2,880.00
  Total:              USD 20,880.00

Datos bancarios para transferencia:
  Banco:              BBVA Bancomer
  CLABE:              012180001234567890
  Referencia:         PG-2024-00847

Ante cualquier duda no dude en contactarnos.

Atentamente,
Cuentas por Cobrar
Proveedor Global S.A.`,
    attachments: [
      { filename: 'Factura_PG-2024-00847.pdf', mimeType: 'application/pdf', sizeBytes: 245_760 },
      { filename: 'Anexo_desglose_horas.xlsx', mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', sizeBytes: 38_912 },
    ],
  },

  Contract: {
    senderName: 'Legal — Corporativo Nexo Internacional',
    senderEmail: 'legal@nexointernacional.com',
    subject: 'Contrato de Servicios Administrados — Para firma y revisión',
    body: `Estimados,

En seguimiento a nuestras negociaciones, adjuntamos el Contrato de Servicios Administrados de TI para su revisión y firma.

Puntos clave del contrato:
  Tipo de acuerdo:    Contrato de Servicios Administrados (MSA)
  Partes:             Nexo Internacional S.A. de C.V. y su organización
  Vigencia:           01/04/2024 — 31/03/2026 (24 meses)
  Renovación:         Automática con 60 días de aviso de no renovación
  Valor total:        USD 480,000.00 anuales
  SLA comprometido:   99.5% uptime mensual
  Cláusula de penalización: 5% del mensual por cada punto porcentual bajo SLA

Obligaciones principales del proveedor:
  - Soporte 24/7 para incidentes críticos (P1/P2)
  - Revisiones mensuales de capacidad y desempeño
  - Entrega de reportes ejecutivos bimestrales
  - Cumplimiento con ISO 27001 e ITIL v4

Le agradecemos nos confirme la recepción y nos comparta sus comentarios antes del 25 de marzo.

Quedamos a sus órdenes,
Departamento Legal
Nexo Internacional`,
    attachments: [
      { filename: 'MSA_Nexo_Internacional_v3.pdf', mimeType: 'application/pdf', sizeBytes: 1_048_576 },
      { filename: 'Anexo_A_Niveles_de_Servicio.pdf', mimeType: 'application/pdf', sizeBytes: 204_800 },
      { filename: 'Anexo_B_Tarifas_2024.xlsx', mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', sizeBytes: 51_200 },
    ],
  },

  'Commercial Proposal': {
    senderName: 'Ventas Corporativas — TechSolutions MX',
    senderEmail: 'ventas@techsolutions.mx',
    subject: 'Propuesta Comercial — Plataforma de Analítica en la Nube — RFP-2024-112',
    body: `Estimado equipo de adquisiciones,

En respuesta a su Solicitud de Propuesta RFP-2024-112, nos complace presentar nuestra propuesta para la implementación de una plataforma de analítica en la nube.

Resumen ejecutivo:
  Solución propuesta:  TechAnalytics Cloud Suite Enterprise
  Implementación:      14 semanas
  Licenciamiento:      Suscripción anual por usuario
  Usuarios incluidos:  150 licencias
  Precio anual:        USD 135,000.00
  Descuento ofrecido:  15% por contrato de 3 años (USD 344,250.00 total)

Nuestra propuesta incluye:
  ✓ Migración de datos históricos (hasta 5 años)
  ✓ Integración con sus sistemas ERP y CRM actuales
  ✓ Capacitación para 20 usuarios administradores
  ✓ Soporte técnico premium durante el primer año
  ✓ Garantía de resultados: ROI mínimo del 200% en 18 meses

Esta oferta es válida hasta el 31 de marzo de 2024.

Con gusto agendamos una demostración en vivo.

Saludos,
Equipo de Ventas Corporativas
TechSolutions MX`,
    attachments: [
      { filename: 'Propuesta_TechAnalytics_RFP-2024-112.pdf', mimeType: 'application/pdf', sizeBytes: 3_145_728 },
      { filename: 'Caso_de_Exito_Cliente_Referencia.pdf', mimeType: 'application/pdf', sizeBytes: 819_200 },
    ],
  },

  'Information Request': {
    senderName: 'María González',
    senderEmail: 'mgonzalez@clienteempresa.com',
    subject: 'Consulta sobre estado de expediente y tiempos de entrega',
    body: `Buen día,

Me pongo en contacto para hacer seguimiento a dos puntos pendientes de nuestra relación comercial:

1. Estado del expediente #EXP-2024-0391:
   Según nuestros registros el expediente fue enviado el 28 de febrero. A la fecha no hemos recibido confirmación de recepción ni número de folio asignado. ¿Podría confirmarnos si está en proceso?

2. Tiempos de entrega para el lote pendiente:
   En el pedido PO-44821 compramos 500 unidades del SKU MON-27-4K con entrega comprometida para la segunda semana de marzo. Necesitamos saber si habrá retraso, ya que tenemos una línea de producción esperando este componente.

3. Documentación requerida:
   Para el cierre contable de Q1 necesitamos que nos envíen los certificados de calidad de los últimos tres envíos (enero, febrero y marzo).

Agradezco su pronta atención a estos puntos.

Saludos cordiales,
María González
Gerente de Compras
Cliente Empresa S.A.
Tel: +52 55 1234 5678`,
    attachments: [],
  },

  Marketing: {
    senderName: 'Equipo CloudWorld Summit 2024',
    senderEmail: 'noreply@cloudworldsummit.com',
    subject: '🚀 CloudWorld Summit 2024 — Últimos lugares disponibles | Descuento del 30%',
    body: `¡Hola!

El evento más importante de tecnología en la nube del año está por llegar y NO quieres perdértelo.

☁️ CLOUDWORLD SUMMIT 2024
📅 Fecha: 18–20 de abril de 2024
📍 Lugar: Centro Citibanamex, Ciudad de México

¿POR QUÉ ASISTIR?
  • +80 sesiones técnicas y keynotes
  • Acceso a demostraciones en vivo de las últimas tendencias en IA y Cloud
  • Networking con más de 5,000 profesionales de TI
  • Certificaciones express en AWS, Azure y Google Cloud
  • Zona de exposición con +120 proveedores tecnológicos

OFERTA ESPECIAL — VÁLIDA HASTA EL 31 DE MARZO:
  Entrada General:    $2,500 MXN → $1,750 MXN (30% OFF)
  Entrada VIP:        $5,000 MXN → $3,500 MXN (30% OFF)
  Código de descuento: CLOUD30

Registra a todo tu equipo con el paquete corporativo (5+ personas) y obtén un 40% adicional.

[REGISTRARME AHORA]

Para darse de baja de estos comunicados, responda con "BAJA" en el asunto.

Equipo de Marketing | CloudWorld Summit 2024`,
    attachments: [
      { filename: 'Programa_CloudWorld_Summit_2024.pdf', mimeType: 'application/pdf', sizeBytes: 1_572_864 },
    ],
  },

  'Bank Statement': {
    senderName: 'BBVA Bancomer — Banca Empresarial',
    senderEmail: 'notificaciones@bbva.com.mx',
    subject: 'Estado de Cuenta Empresarial — Febrero 2024 — Cuenta ***4521',
    body: `Estimado cliente,

Le informamos que su estado de cuenta del mes de febrero de 2024 ya está disponible.

RESUMEN DE CUENTA
  Número de cuenta:   ****-****-****-4521
  Tipo de cuenta:     Cuenta Corriente Empresarial
  Período:            01/02/2024 — 29/02/2024
  Moneda:             MXN

MOVIMIENTOS DEL PERÍODO
  Saldo inicial:      $1,245,830.45
  Total cargos:       $   892,150.00
  Total abonos:       $   435,200.00
  Saldo final:        $   788,880.45

CARGOS PRINCIPALES:
  04/02  Pago a proveedores batch      $  245,000.00
  10/02  Transferencia SPEI saliente   $  380,000.00
  15/02  Domiciliación servicios       $   47,150.00
  28/02  Comisiones bancarias          $   15,000.00

ABONOS PRINCIPALES:
  07/02  Depósito cliente ABC Corp     $  200,000.00
  14/02  Transferencia SPEI entrante   $  135,000.00
  21/02  Intereses generados           $      200.00

Su estado de cuenta completo está adjunto en formato PDF.
Por seguridad, nunca compartimos contraseñas por correo electrónico.

Banca Empresarial BBVA México`,
    attachments: [
      { filename: 'Estado_Cuenta_Feb2024_4521.pdf', mimeType: 'application/pdf', sizeBytes: 614_400 },
    ],
  },

  Unknown: {
    senderName: 'Roberto Méndez',
    senderEmail: 'rmendez@ejemplo.com',
    subject: 'RE: FWD: Actualización importante',
    body: `Hola,

En seguimiento al correo anterior, quisiera saber si ya tuviste oportunidad de revisar lo que te enviamos la semana pasada.

Quedo pendiente.

Saludos
Roberto

--- Mensaje original ---
De: Ana Torres
Para: Roberto Méndez
Asunto: FWD: Actualización importante

Fwd del doc que me pasó Marcos.

--- Mensaje reenviado ---
De: Marcos Ruiz
Para: Ana Torres

Ana, comparte esto con quien corresponda.

[El archivo original no fue incluido en este reenvío]`,
    attachments: [],
  },
}

const CATEGORIES = Object.keys(TEMPLATES) as (keyof typeof TEMPLATES)[]

const CATEGORY_STYLES: Record<string, string> = {
  Invoice:               'bg-violet-50 text-violet-700 border-violet-200 hover:bg-violet-100',
  Contract:              'bg-indigo-50 text-indigo-700 border-indigo-200 hover:bg-indigo-100',
  'Commercial Proposal': 'bg-sky-50 text-sky-700 border-sky-200 hover:bg-sky-100',
  'Information Request': 'bg-cyan-50 text-cyan-700 border-cyan-200 hover:bg-cyan-100',
  Marketing:             'bg-pink-50 text-pink-700 border-pink-200 hover:bg-pink-100',
  'Bank Statement':      'bg-emerald-50 text-emerald-700 border-emerald-200 hover:bg-emerald-100',
  Unknown:               'bg-gray-50 text-gray-600 border-gray-200 hover:bg-gray-100',
}

// ── Attachment row ────────────────────────────────────────────────────────────

function AttachmentRow({
  att,
  onChange,
  onRemove,
}: {
  att: AttachmentIngestDto
  onChange: (next: AttachmentIngestDto) => void
  onRemove: () => void
}) {
  return (
    <div className="flex items-center gap-2 rounded-lg border border-gray-200 bg-gray-50 px-3 py-2">
      <span className="text-sm text-gray-400 select-none">📎</span>
      <input
        className="min-w-0 flex-1 bg-transparent text-sm text-gray-700 outline-none placeholder:text-gray-300"
        placeholder="filename.pdf"
        value={att.filename}
        onChange={e => onChange({ ...att, filename: e.target.value })}
      />
      <select
        className="shrink-0 rounded border border-gray-200 bg-white px-1.5 py-0.5 text-xs text-gray-600 outline-none"
        value={att.mimeType}
        onChange={e => onChange({ ...att, mimeType: e.target.value })}
      >
        <option value="application/pdf">PDF</option>
        <option value="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet">Excel</option>
        <option value="application/vnd.openxmlformats-officedocument.wordprocessingml.document">Word</option>
        <option value="text/plain">TXT</option>
        <option value="text/html">HTML</option>
        <option value="image/png">PNG</option>
        <option value="image/jpeg">JPG</option>
      </select>
      <button
        type="button"
        onClick={onRemove}
        className="ml-1 text-gray-300 hover:text-red-400 transition-colors"
        aria-label="Remove attachment"
      >
        ×
      </button>
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

interface FormState {
  senderName: string
  senderEmail: string
  subject: string
  body: string
  attachments: AttachmentIngestDto[]
}

const EMPTY: FormState = {
  senderName: '',
  senderEmail: '',
  subject: '',
  body: '',
  attachments: [],
}

export function SimulatorPage() {
  const [form, setForm] = useState<FormState>(EMPTY)
  const [activeCategory, setActiveCategory] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () =>
      emailApi.ingest({
        senderName: form.senderName,
        senderEmail: form.senderEmail,
        subject: form.subject,
        bodyPlainText: form.body,
        attachments: form.attachments.length > 0 ? form.attachments : undefined,
      }),
  })

  function applyTemplate(category: string) {
    const tpl = TEMPLATES[category]
    if (!tpl) return
    setForm({ ...tpl })
    setActiveCategory(category)
    mutation.reset()
  }

  function setField<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm(prev => ({ ...prev, [key]: value }))
    mutation.reset()
  }

  function addAttachment() {
    setField('attachments', [
      ...form.attachments,
      { filename: '', mimeType: 'application/pdf', sizeBytes: 0 },
    ])
  }

  function updateAttachment(i: number, next: AttachmentIngestDto) {
    const updated = [...form.attachments]
    updated[i] = next
    setField('attachments', updated)
  }

  function removeAttachment(i: number) {
    setField('attachments', form.attachments.filter((_, idx) => idx !== i))
  }

  const canSubmit =
    form.senderEmail.trim() !== '' &&
    form.subject.trim() !== '' &&
    form.body.trim() !== '' &&
    !mutation.isPending

  return (
    <div className="flex h-full flex-col">
      {/* Header bar */}
      <div className="flex items-center gap-3 border-b border-gray-200 bg-white px-6 py-3">
        <span className="text-sm font-semibold text-gray-800">Email Simulator</span>
        <span className="text-xs text-gray-400">
          Genera y envía correos de prueba al pipeline de agentes
        </span>
      </div>

      <div className="flex-1 overflow-auto p-6">
        <div className="mx-auto max-w-2xl space-y-5">

          {/* Category quick-fill */}
          <div className="rounded-lg border border-gray-200 bg-white p-5">
            <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-gray-400">
              Generar por categoría
            </p>
            <div className="flex flex-wrap gap-2">
              {CATEGORIES.map(cat => (
                <button
                  key={cat}
                  type="button"
                  onClick={() => applyTemplate(cat)}
                  className={cn(
                    'rounded-full border px-3 py-1.5 text-xs font-medium transition-all',
                    activeCategory === cat
                      ? cn(CATEGORY_STYLES[cat], 'ring-2 ring-offset-1 ring-current')
                      : CATEGORY_STYLES[cat],
                  )}
                >
                  {cat}
                </button>
              ))}
            </div>
            {activeCategory && (
              <p className="mt-3 text-xs text-gray-400">
                Plantilla <strong className="text-gray-600">{activeCategory}</strong> cargada — puedes editar los campos antes de enviar.
              </p>
            )}
          </div>

          {/* Form */}
          <div className="rounded-lg border border-gray-200 bg-white p-5 space-y-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-gray-400">
              Datos del correo
            </p>

            {/* Sender row */}
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="mb-1 block text-xs text-gray-500">Nombre del remitente</label>
                <input
                  className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm text-gray-800 outline-none focus:border-blue-400 focus:ring-1 focus:ring-blue-100 placeholder:text-gray-300"
                  placeholder="Acme Corp — Finanzas"
                  value={form.senderName}
                  onChange={e => setField('senderName', e.target.value)}
                />
              </div>
              <div>
                <label className="mb-1 block text-xs text-gray-500">
                  Email del remitente <span className="text-red-400">*</span>
                </label>
                <input
                  type="email"
                  className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm text-gray-800 outline-none focus:border-blue-400 focus:ring-1 focus:ring-blue-100 placeholder:text-gray-300"
                  placeholder="remitente@empresa.com"
                  value={form.senderEmail}
                  onChange={e => setField('senderEmail', e.target.value)}
                />
              </div>
            </div>

            {/* Subject */}
            <div>
              <label className="mb-1 block text-xs text-gray-500">
                Asunto <span className="text-red-400">*</span>
              </label>
              <input
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm text-gray-800 outline-none focus:border-blue-400 focus:ring-1 focus:ring-blue-100 placeholder:text-gray-300"
                placeholder="Factura #001 — Servicios de enero"
                value={form.subject}
                onChange={e => setField('subject', e.target.value)}
              />
            </div>

            {/* Body */}
            <div>
              <label className="mb-1 block text-xs text-gray-500">
                Cuerpo del correo <span className="text-red-400">*</span>
              </label>
              <textarea
                rows={14}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm text-gray-800 outline-none focus:border-blue-400 focus:ring-1 focus:ring-blue-100 placeholder:text-gray-300 resize-y font-mono leading-relaxed"
                placeholder="Escriba el contenido del correo…"
                value={form.body}
                onChange={e => setField('body', e.target.value)}
              />
            </div>

            {/* Attachments */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <label className="text-xs text-gray-500">Adjuntos (opcional)</label>
                <button
                  type="button"
                  onClick={addAttachment}
                  className="flex items-center gap-1 rounded-md border border-dashed border-gray-300 px-2.5 py-1 text-xs text-gray-400 hover:border-blue-300 hover:text-blue-500 transition-colors"
                >
                  + Agregar adjunto
                </button>
              </div>

              {form.attachments.length > 0 ? (
                <div className="space-y-2">
                  {form.attachments.map((att, i) => (
                    <AttachmentRow
                      key={i}
                      att={att}
                      onChange={next => updateAttachment(i, next)}
                      onRemove={() => removeAttachment(i)}
                    />
                  ))}
                </div>
              ) : (
                <p className="text-xs italic text-gray-300 py-1">Sin adjuntos</p>
              )}
            </div>
          </div>

          {/* Actions */}
          <div className="flex items-center justify-between gap-4">
            <button
              type="button"
              onClick={() => { setForm(EMPTY); setActiveCategory(null); mutation.reset() }}
              className="text-xs text-gray-400 hover:text-gray-600 transition-colors"
            >
              Limpiar formulario
            </button>

            <button
              type="button"
              onClick={() => mutation.mutate()}
              disabled={!canSubmit}
              className={cn(
                'flex items-center gap-2 rounded-lg px-5 py-2.5 text-sm font-medium transition-all',
                canSubmit
                  ? 'bg-blue-600 text-white hover:bg-blue-700 shadow-sm'
                  : 'bg-gray-100 text-gray-400 cursor-not-allowed',
              )}
            >
              {mutation.isPending ? (
                <>
                  <span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-white border-t-transparent" />
                  Enviando…
                </>
              ) : (
                'Enviar al Inbox →'
              )}
            </button>
          </div>

          {/* Success */}
          {mutation.isSuccess && (
            <div className="rounded-lg border border-green-200 bg-green-50 px-5 py-4">
              <p className="text-sm font-medium text-green-800">
                ✓ Correo enviado al pipeline
              </p>
              <p className="mt-1 text-xs text-green-600">
                ID: <span className="font-mono">{mutation.data.emailId}</span>
                {' · '}Estado inicial: <strong>{mutation.data.status}</strong>
              </p>
              <div className="mt-3 flex gap-3">
                <Link
                  to={`/inbox/${mutation.data.emailId}`}
                  className="text-xs font-medium text-green-700 underline hover:text-green-900"
                >
                  Ver en Inbox →
                </Link>
                <button
                  type="button"
                  onClick={() => { setForm(EMPTY); setActiveCategory(null); mutation.reset() }}
                  className="text-xs text-green-600 hover:text-green-800"
                >
                  Enviar otro
                </button>
              </div>
            </div>
          )}

          {/* Error */}
          {mutation.isError && (
            <div className="rounded-lg border border-red-200 bg-red-50 px-5 py-4">
              <p className="text-sm font-medium text-red-800">Error al enviar el correo</p>
              <p className="mt-1 text-xs text-red-600">
                {mutation.error instanceof Error
                  ? mutation.error.message
                  : 'Error desconocido. Verifica que el API esté en ejecución.'}
              </p>
            </div>
          )}

        </div>
      </div>
    </div>
  )
}
