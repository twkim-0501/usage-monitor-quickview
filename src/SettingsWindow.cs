using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UsageMonitorQuickView;

public sealed class SettingsWindow : Window
{
    public SettingsWindow(UsageMonitorSettings settings)
    {
        Title = "Usage Monitor Quick View · 설정"; Width = 440; Height = 390; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = (Brush)new BrushConverter().ConvertFromString("#FAFAFC")!;
        var stack = new StackPanel { Margin = new Thickness(24) }; Content = stack;
        stack.Children.Add(new TextBlock { Text = "Usage Monitor", FontSize = 22, FontWeight = FontWeights.SemiBold });
        stack.Children.Add(new TextBlock { Text = "웹 대시보드 주소 · HTTP/HTTPS", FontSize = 11, Margin = new Thickness(0, 20, 0, 6) });
        var url = new TextBox { Text = settings.Url ?? "" }; stack.Children.Add(url);
        stack.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(settings.HelperExecutable) ? "주소에 비밀번호나 토큰을 넣지 마세요. 사이트의 로그인 화면은 웹 창 안에서 이용할 수 있습니다." : "기존 접속 도우미가 연결되어 있습니다. 주소를 비워두면 저장된 로그인과 SSH 연결을 사용합니다.", TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 10, 0, 8) });
        var dock = new CheckBox { Content = "작업표시줄 안에 붙이기", IsChecked = settings.DockButton }; stack.Children.Add(dock);
        var startup = new CheckBox { Content = "Windows 로그인 시 아이콘만 표시", IsChecked = settings.StartWithWindows }; stack.Children.Add(startup);
        var save = new Button { Content = "저장", Margin = new Thickness(0, 14, 0, 0) }; stack.Children.Add(save);
        save.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(url.Text))
            {
                try { DashboardLauncher.ValidateAddress(url.Text.Trim()); }
                catch (InvalidOperationException error) { MessageBox.Show(error.Message, Title); return; }
            }
            else if (string.IsNullOrWhiteSpace(settings.HelperExecutable)) { MessageBox.Show("웹 대시보드 주소를 입력하세요.", Title); return; }
            settings.Url = string.IsNullOrWhiteSpace(url.Text) ? null : url.Text.Trim();
            settings.DockButton = dock.IsChecked == true; settings.StartWithWindows = startup.IsChecked == true; DialogResult = true;
        };
    }
}
