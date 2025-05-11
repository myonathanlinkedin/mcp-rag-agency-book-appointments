import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { ChakraProvider, ColorModeScript } from '@chakra-ui/react';
import { AuthProvider } from '@/contexts/AuthContext';
import theme from '@/theme';
import ErrorBoundary from '@/components/ErrorBoundary';

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
});

export const metadata: Metadata = {
  title: "Agent Book - Intelligent Chat Assistant",
  description: "Book appointments and get assistance through our intelligent chat interface",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body className={`${inter.variable} antialiased`}>
        <ColorModeScript initialColorMode="light" />
        <ErrorBoundary>
          <ChakraProvider theme={theme}>
            <AuthProvider>{children}</AuthProvider>
          </ChakraProvider>
        </ErrorBoundary>
      </body>
    </html>
  );
}
