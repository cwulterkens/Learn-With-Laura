import AppKit
import AVFoundation
import CoreVideo

let args = CommandLine.arguments
guard args.count >= 3 else {
    fatalError("Usage: swift create_credits_video.swift input.png output.mov")
}

let inputUrl = URL(fileURLWithPath: args[1])
let outputUrl = URL(fileURLWithPath: args[2])
try? FileManager.default.removeItem(at: outputUrl)

guard let sourceImage = NSImage(contentsOf: inputUrl) else {
    fatalError("Could not open input image")
}

let width = 1920
let height = 1080
let fps = 30
let durationSeconds = 4
let totalFrames = fps * durationSeconds

let writer = try AVAssetWriter(outputURL: outputUrl, fileType: .mov)
let settings: [String: Any] = [
    AVVideoCodecKey: AVVideoCodecType.h264,
    AVVideoWidthKey: width,
    AVVideoHeightKey: height,
    AVVideoCompressionPropertiesKey: [
        AVVideoAverageBitRateKey: 8_000_000,
        AVVideoProfileLevelKey: AVVideoProfileLevelH264HighAutoLevel
    ]
]

let input = AVAssetWriterInput(mediaType: .video, outputSettings: settings)
input.expectsMediaDataInRealTime = false

let adaptor = AVAssetWriterInputPixelBufferAdaptor(
    assetWriterInput: input,
    sourcePixelBufferAttributes: [
        kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32ARGB,
        kCVPixelBufferWidthKey as String: width,
        kCVPixelBufferHeightKey as String: height
    ]
)

writer.add(input)
writer.startWriting()
writer.startSession(atSourceTime: .zero)

func makeBuffer(frame: Int) -> CVPixelBuffer {
    var buffer: CVPixelBuffer?
    CVPixelBufferCreate(
        kCFAllocatorDefault,
        width,
        height,
        kCVPixelFormatType_32ARGB,
        nil,
        &buffer
    )
    guard let pixelBuffer = buffer else {
        fatalError("Could not create pixel buffer")
    }

    CVPixelBufferLockBaseAddress(pixelBuffer, [])
    let context = CGContext(
        data: CVPixelBufferGetBaseAddress(pixelBuffer),
        width: width,
        height: height,
        bitsPerComponent: 8,
        bytesPerRow: CVPixelBufferGetBytesPerRow(pixelBuffer),
        space: CGColorSpaceCreateDeviceRGB(),
        bitmapInfo: CGImageAlphaInfo.noneSkipFirst.rawValue
    )!

    let t = CGFloat(frame) / CGFloat(totalFrames - 1)
    let fadeIn = min(1.0, t / 0.18)
    let fadeOut = min(1.0, (1.0 - t) / 0.18)
    let alpha = max(0.0, min(fadeIn, fadeOut))
    let zoom = 1.0 + 0.035 * t

    context.setFillColor(NSColor.black.cgColor)
    context.fill(CGRect(x: 0, y: 0, width: width, height: height))

    if let cgImage = sourceImage.cgImage(forProposedRect: nil, context: nil, hints: nil) {
        context.saveGState()
        context.setAlpha(alpha)
        let imageAspect = CGFloat(cgImage.width) / CGFloat(cgImage.height)
        let canvasAspect = CGFloat(width) / CGFloat(height)
        var drawWidth = CGFloat(width)
        var drawHeight = CGFloat(height)
        if imageAspect > canvasAspect {
            drawHeight = drawWidth / imageAspect
        } else {
            drawWidth = drawHeight * imageAspect
        }
        drawWidth *= zoom
        drawHeight *= zoom
        let rect = CGRect(
            x: (CGFloat(width) - drawWidth) / 2,
            y: (CGFloat(height) - drawHeight) / 2,
            width: drawWidth,
            height: drawHeight
        )
        context.draw(cgImage, in: rect)
        context.restoreGState()
    }

    CVPixelBufferUnlockBaseAddress(pixelBuffer, [])
    return pixelBuffer
}

let frameDuration = CMTime(value: 1, timescale: CMTimeScale(fps))
for frame in 0..<totalFrames {
    while !input.isReadyForMoreMediaData {
        Thread.sleep(forTimeInterval: 0.01)
    }
    let buffer = makeBuffer(frame: frame)
    let time = CMTimeMultiply(frameDuration, multiplier: Int32(frame))
    adaptor.append(buffer, withPresentationTime: time)
}

input.markAsFinished()
let semaphore = DispatchSemaphore(value: 0)
writer.finishWriting {
    semaphore.signal()
}
semaphore.wait()

if writer.status != .completed {
    fatalError("Video writer failed: \(writer.error?.localizedDescription ?? "unknown error")")
}
