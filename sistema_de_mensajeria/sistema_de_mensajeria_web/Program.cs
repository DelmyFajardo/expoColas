using System.Text;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SistemaDeMensajeriaWeb;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddSingleton<ConcurrentQueue<string>>();
builder.Services.AddHostedService<RabbitMQConsumerService>();

var app = builder.Build();

// Ruta para mostrar los mensajes
app.MapGet("/", async (HttpContext context, ConcurrentQueue<string> messages) => {
    context.Response.ContentType = "text/html; charset=utf-8";
    
    var html = new StringBuilder();
  html.Append(@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Sistema de Mensajería</title>
    <meta http-equiv='refresh' content='5'>
    <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600&display=swap' rel='stylesheet'>
    <style>
        :root {
            --primary-color: #0ea5e9;
            --secondary-color: #0369a1;
            --accent-color: #06b6d4;
            --background-color: #f1f5f9;
            --text-color: #0f172a;
            --card-bg: #ffffff;
            --shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
            --border-radius: 16px;
        }

        body {
            font-family: 'Inter', system-ui, sans-serif;
            margin: 0;
            min-height: 100vh;
            background: linear-gradient(135deg, var(--background-color), #e2e8f0);
            color: var(--text-color);
            line-height: 1.6;
        }

        .container {
            max-width: 1000px;
            margin: 0 auto;
            padding: 2rem;
        }

        header {
            background: var(--card-bg);
            padding: 2rem;
            border-radius: var(--border-radius);
            box-shadow: var(--shadow);
            margin-bottom: 2rem;
            position: relative;
            overflow: hidden;
        }

        header::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 4px;
            background: linear-gradient(90deg, var(--primary-color), var(--accent-color));
        }

        h1 {
            color: var(--primary-color);
            font-size: 2.5rem;
            margin: 0;
            font-weight: 600;
        }

        .subtitle {
            color: var(--secondary-color);
            font-size: 1.1rem;
            margin-top: 0.5rem;
        }

        .connection-badge {
            display: inline-flex;
            align-items: center;
            background: #dcfce7;
            color: #166534;
            padding: 0.5rem 1rem;
            border-radius: 9999px;
            font-size: 0.875rem;
            font-weight: 500;
            margin-top: 1rem;
        }

        .status-indicator {
            width: 8px;
            height: 8px;
            background-color: #22c55e;
            border-radius: 50%;
            margin-right: 0.5rem;
            animation: pulse 2s infinite;
        }

        .message-container {
            background: var(--card-bg);
            border-radius: var(--border-radius);
            box-shadow: var(--shadow);
            overflow: hidden;
        }

        .message-header {
            padding: 1.5rem;
            background: linear-gradient(90deg, var(--primary-color), var(--accent-color));
            color: white;
        }

        .message-list {
            list-style: none;
            margin: 0;
            padding: 0;
        }

        .message-item {
            padding: 1rem 1.5rem;
            border-bottom: 1px solid #e2e8f0;
            display: flex;
            align-items: center;
            gap: 1rem;
            transition: all 0.2s;
        }

        .message-item:hover {
            background-color: #f8fafc;
        }

        .timestamp {
            background: #e0f2fe;
            color: var(--primary-color);
            padding: 0.25rem 0.75rem;
            border-radius: 9999px;
            font-size: 0.875rem;
            font-weight: 500;
            white-space: nowrap;
        }

        .message-content {
            flex: 1;
            font-size: 1rem;
        }

        .empty-state {
            padding: 4rem 2rem;
            text-align: center;
            color: #64748b;
        }

        .empty-state svg {
            width: 64px;
            height: 64px;
            color: var(--primary-color);
            margin-bottom: 1rem;
        }

        footer {
            margin-top: 2rem;
            text-align: center;
            color: #64748b;
        }

        .tech-stack {
            display: flex;
            gap: 0.5rem;
            justify-content: center;
            margin-bottom: 1rem;
        }

        .tech-badge {
            background: var(--primary-color);
            color: white;
            padding: 0.5rem 1rem;
            border-radius: 9999px;
            font-size: 0.875rem;
            font-weight: 500;
            transition: transform 0.2s;
        }

        .tech-badge:hover {
            transform: translateY(-2px);
        }

        @media (prefers-color-scheme: dark) {
            :root {
                --background-color: #0f172a;
                --text-color: #f1f5f9;
                --card-bg: #1e293b;
            }

            .message-item:hover {
                background-color: #334155;
            }
        }

        @media (max-width: 768px) {
            .container {
                padding: 1rem;
            }

            header {
                padding: 1.5rem;
            }

            h1 {
                font-size: 2rem;
            }
        }
    </style>
</head>
<body>
    <div class='container'>
        <header>
            <h1>Sistema de Mensajería</h1>
            <div class='subtitle'>Mensajes en tiempo real vía RabbitMQ</div>
            <div class='connection-badge'>
                <span class='status-indicator'></span>
                Conexión activa
            </div>
        </header>

        <div class='message-container'>
            <div class='message-header'>
                <h2 style='margin:0;font-size:1.25rem'>Mensajes Recientes</h2>
            </div>");

    
        if (messages.IsEmpty)
        {
            html.Append(@"
                <div class='empty-state'>
                    <svg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 24 24' stroke='currentColor'>
                        <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z' />
                    </svg>
                    <p>No hay mensajes recibidos.</p>
                    <p>Los mensajes aparecerán aquí cuando se envíen desde la aplicación de consola.</p>
                </div>");
        }
        else
        {
            html.Append(@"
                <ul class='message-list'>");
    
            foreach (var message in messages)
            {
                var parts = message.Split(": ", 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    html.Append($@"
                    <li class='message-item'>
                        <span class='timestamp'>{parts[0]}</span>
                        <span class='message-content'>{parts[1]}</span>
                    </li>");
                }
                else
                {
                    html.Append($@"<li class='message-item'><span class='message-content'>{message}</span></li>");
                }
            }
    
            html.Append(@"
                </ul>");
        }
    
        html.Append(@"
            </div>
    
            <footer>
                <div>
                    <span class='tech-badge'>RabbitMQ</span>
                    <span class='tech-badge'>.NET</span>
                    <span class='tech-badge'>C#</span>
                    <span class='tech-badge'>Docker</span>
                </div>
                <div style='margin-top: 10px;'>
                    &copy; " + DateTime.Now.Year + @" Sistema de Mensajería
                </div>
            </footer>
        </div>
    </body>
    </html>");

    await context.Response.WriteAsync(html.ToString());
});

app.Run();