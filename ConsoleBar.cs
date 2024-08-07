using ShellProgressBar;

namespace IAG;

public class ConsoleBar: IDisposable
{
    private ProgressBar pBar;
    private int curTick = 0;
    private int maxTicks;
    public ConsoleBar(int maxTicks, string message)
    {
        var options = new ProgressBarOptions()
        {
            ForegroundColor = ConsoleColor.White,
            ProgressBarOnBottom = true,
            ProgressCharacter = '-'
        };
        this.maxTicks = maxTicks;
        pBar = new ProgressBar(maxTicks, message, options);
        
    }
    public async Task RunAsync(CancellationToken token)
    {
        try
        {
            while (curTick < maxTicks)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(100);
                pBar.Tick();
                curTick++;
            }
        }
        catch (OperationCanceledException)
        {
            Dispose();
        }
    }
    public void Dispose()
    {
        if(curTick < maxTicks)
        {
            pBar.Tick(maxTicks);
            curTick = maxTicks;
        }
        pBar.Dispose();
    }
}