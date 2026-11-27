using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Volante;

// ---------------- Estruturas do Interception ----------------

[StructLayout(LayoutKind.Sequential)]
struct MouseStroke
{
    public ushort state;
    public ushort flags;
    public short rolling;
    public int x;
    public int y;
    public uint information;
}

[StructLayout(LayoutKind.Explicit, Size = 20)]
struct Stroke
{
    [FieldOffset(0)]
    public MouseStroke mouse;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int PredicadoInterception(int device);

// ---------------- Interception ----------------

static class Interception
{
    const string Dll = "interception.dll";

    public const ushort FiltroMouseTudo = 0xFFFF;

    const int PrimeiroMouse = 11;
    const int UltimoMouse = 20;

    [DllImport(Dll, EntryPoint = "interception_create_context", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CriarContexto();

    [DllImport(Dll, EntryPoint = "interception_destroy_context", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DestruirContexto(IntPtr contexto);

    // O hardware ID vem em UTF-16, por isso o buffer é byte[].
    [DllImport(Dll, EntryPoint = "interception_get_hardware_id", CallingConvention = CallingConvention.Cdecl)]
    static extern uint ObterHardwareId(IntPtr contexto, int dispositivo, byte[] buffer, uint tamanho);

    [DllImport(Dll, EntryPoint = "interception_set_filter", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DefinirFiltro(IntPtr contexto, PredicadoInterception predicado, ushort filtro);

    [DllImport(Dll, EntryPoint = "interception_wait_with_timeout", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Esperar(IntPtr contexto, uint milissegundos);

    [DllImport(Dll, EntryPoint = "interception_receive", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Receber(IntPtr contexto, int dispositivo, [In, Out] Stroke[] strokes, uint quantidade);

    /// <summary>Retorna o número do dispositivo (11 a 20) ou 0 se não achar.</summary>
    public static int EncontrarMouse(IntPtr contexto, string hardwareId)
    {
        for (int i = PrimeiroMouse; i <= UltimoMouse; i++)
        {
            if (MouseConectado(contexto, i, hardwareId))
                return i;
        }

        return 0;
    }

    public static bool MouseConectado(IntPtr contexto, int dispositivo, string hardwareId)
    {
        if (dispositivo < PrimeiroMouse || dispositivo > UltimoMouse)
            return false;

        byte[] buffer = new byte[1000];
        uint tamanho = ObterHardwareId(contexto, dispositivo, buffer, (uint)buffer.Length);

        if (tamanho == 0)
            return false;

        string id = Encoding.Unicode.GetString(
            buffer, 0, (int)Math.Min(tamanho, (uint)buffer.Length));

        return id.Contains(hardwareId, StringComparison.OrdinalIgnoreCase);
    }
}

// ---------------- vJoy ----------------

static class VJoy
{
    const string Dll = "vJoyInterface.dll";

    public const uint EixoX = 0x30;

    [DllImport(Dll, EntryPoint = "vJoyEnabled", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool Habilitado();

    [DllImport(Dll, EntryPoint = "AcquireVJD", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool Adquirir(uint id);

    [DllImport(Dll, EntryPoint = "RelinquishVJD", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Liberar(uint id);

    [DllImport(Dll, EntryPoint = "ResetVJD", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool Zerar(uint id);

    [DllImport(Dll, EntryPoint = "GetVJDAxisMin", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LimiteMinimo(uint id, uint eixo, out int minimo);

    [DllImport(Dll, EntryPoint = "GetVJDAxisMax", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LimiteMaximo(uint id, uint eixo, out int maximo);

    [DllImport(Dll, EntryPoint = "SetAxis", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DefinirEixo(int valor, uint id, uint eixo);

    [DllImport(Dll, EntryPoint = "SetBtn", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DefinirBotao(bool pressionado, uint id, byte botao);
}

// ---------------- Teclado global ----------------

static class Teclado
{
    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int teclaVirtual);

    public static bool Pressionada(int teclaVirtual)
    {
        return (GetAsyncKeyState(teclaVirtual) & 0x8000) != 0;
    }
}
