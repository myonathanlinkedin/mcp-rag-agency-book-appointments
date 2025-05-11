import { NextResponse } from 'next/server';
import { sign } from 'jsonwebtoken';

const JWT_SECRET = process.env.JWT_SECRET || 'your-secret-key';

export async function POST(request: Request) {
  try {
    const { email, password } = await request.json();

    // For testing, accept any email/password combination
    // In production, you would validate against a database
    if (!email || !password) {
      return NextResponse.json(
        { error: 'Email and password are required' },
        { status: 400 }
      );
    }

    // Create mock tokens
    const accessToken = sign(
      { sub: '1', email },
      JWT_SECRET,
      { expiresIn: '1h' }
    );
    const refreshToken = sign(
      { sub: '1', email },
      JWT_SECRET,
      { expiresIn: '7d' }
    );

    return NextResponse.json({
      accessToken,
      refreshToken,
    });
  } catch (error) {
    console.error('Login error:', error);
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    );
  }
} 