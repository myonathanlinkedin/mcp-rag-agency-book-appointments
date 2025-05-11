import { NextResponse } from 'next/server';
import { sign, verify } from 'jsonwebtoken';

const JWT_SECRET = process.env.JWT_SECRET || 'your-secret-key';

export async function POST(request: Request) {
  try {
    const { refreshToken } = await request.json();

    if (!refreshToken) {
      return NextResponse.json(
        { error: 'Refresh token is required' },
        { status: 400 }
      );
    }

    try {
      // Verify the refresh token
      const decoded = verify(refreshToken, JWT_SECRET) as { sub: string; email: string };

      // Create a new access token
      const accessToken = sign(
        { sub: decoded.sub, email: decoded.email },
        JWT_SECRET,
        { expiresIn: '1h' }
      );

      return NextResponse.json({ accessToken });
    } catch (error) {
      return NextResponse.json(
        { error: 'Invalid refresh token' },
        { status: 401 }
      );
    }
  } catch (error) {
    console.error('Refresh token error:', error);
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    );
  }
} 