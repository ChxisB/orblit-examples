import Cocoa
import FlutterMacOS

class MainFlutterWindow: NSWindow {
  override func awakeFromNib() {
    let flutterViewController = FlutterViewController()
    self.contentViewController = flutterViewController

    RegisterGeneratedPlugins(registry: flutterViewController)

    super.awakeFromNib()

    // Once everything else has had its turn. The nib sizes the window, the
    // superclass applies that, and macOS restores whatever frame it saved
    // last time — so a size set before any of those is a size that does not
    // survive.
    DispatchQueue.main.async { [weak self] in self?.sizeToGallery() }
  }

  private func sizeToGallery() {
    let wanted = NSSize(width: 1500, height: 940)
    if let screen = NSScreen.main {
      let area = screen.visibleFrame
      let size = NSSize(
        width: min(wanted.width, area.width - 40),
        height: min(wanted.height, area.height - 40))
      setFrame(
        NSRect(
          x: area.minX + (area.width - size.width) / 2,
          y: area.minY + (area.height - size.height) / 2,
          width: size.width,
          height: size.height),
        display: true)

      // Where it ended up, for anything driving this from outside — a
      // screenshot in a document, or a build that photographs every example.
      // Behind a variable, so an ordinary run says nothing.
      if ProcessInfo.processInfo.environment["ORBLIT_WINDOW_FRAME"] != nil {
        let f = frame
        // Reported with the origin at the top left, which is where a screen
        // capture puts it and where a window's own frame does not.
        //
        // On stdout rather than through NSLog, because the only thing that
        // reads this is a script that launched the binary and is watching its
        // output. NSLog goes to the unified log, where that script would have
        // to go looking for it by process and timestamp — and where, from a
        // release build, it may not arrive at all. Flushed because stdout to
        // a pipe is block-buffered, and a line that arrives when the app
        // quits is a line that arrives after the recording.
        print(
          "[orblit] window \(Int(f.minX)),\(Int(screen.frame.height - f.maxY)) "
            + "\(Int(f.width))x\(Int(f.height))")
        fflush(stdout)
      }
    }
  }
}
